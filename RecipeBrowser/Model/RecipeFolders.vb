Imports Windows.UI.Popups
Imports Windows.Storage
Imports Windows.Storage.Provider
Imports RecipeBrowser.Persistency

Public Class FolderDescriptor
    Public CategoryPath As String
    Public IndicatorColor As SolidColorBrush = New SolidColorBrush(Windows.UI.Colors.Transparent)
    Public FontColor As SolidColorBrush = App.Current.Resources("MenuBarForegroundBrush")
    Public CategoryWithIndent As String
End Class

Public Class RecipeFolders

    Public Shared Current As RecipeFolders

    Private _folders As New ObservableCollection(Of RecipeFolder)()
    Public ReadOnly Property Folders As ObservableCollection(Of RecipeFolder)
        Get
            Return _folders
        End Get
    End Property

    Private _subFolders As New List(Of RecipeFolder)()
    Public ReadOnly Property SubFolders As List(Of RecipeFolder)
        Get
            Return _subFolders
        End Get
    End Property

    Private _tagFolders As New ObservableCollection(Of TagFolder)()
    Public ReadOnly Property TagFolders As ObservableCollection(Of TagFolder)
        Get
            Return _tagFolders
        End Get
    End Property

    Public CategoryHierarchy As New List(Of FolderDescriptor)

    Public Property FavoriteFolder As Favorites
    Public Property SearchResultsFolder As SearchResults
    Public Property HistoryFolder As History
    Public Property HelpFolder As HelpDocuments

    Private initialized As Boolean

    Public Function ContentLoaded() As Boolean
        Return initialized
    End Function

    Private rootFolder As Windows.Storage.StorageFolder

    Public Shared Function GetInstance() As RecipeFolders
        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)
        Return categories
    End Function

    Public Sub New()
        Current = Me
    End Sub


    Public Async Function GetFolderAsync(name As String, Optional ByVal ReadContent As Boolean = True) As Task(Of RecipeFolder)

        Dim folder = GetFolder(name)

        If ReadContent AndAlso folder IsNot Nothing AndAlso Not folder.ContentLoaded Then
            Await folder.LoadAsync()
        End If

        Return folder

    End Function

    Public Function GetFolder(name As String) As RecipeFolder

        If name = FavoriteFolder.Name Then
            Return FavoriteFolder
        ElseIf name = SearchResults.FolderName Then
            Return SearchResultsFolder
        ElseIf name = History.FolderName Then
            Return HistoryFolder
        ElseIf name = HelpDocuments.FolderName Then
            Return HelpFolder
        Else
            Dim matches = _folders.Where(Function(otherFolder) otherFolder.Name.Equals(name))
            If matches.Count() = 1 Then
                Dim folder = matches.First()
                Return folder
            End If
            Dim subFoldermatches = _subFolders.Where(Function(otherFolder) otherFolder.Name.Equals(name))
            If subFoldermatches.Count() = 1 Then
                Dim folder = subFoldermatches.First()
                Return folder
            End If
            Dim tagFoldermatches = _tagFolders.Where(Function(otherFolder) otherFolder.Name.Equals(name))
            If tagFoldermatches.Count() = 1 Then
                Dim folder = tagFoldermatches.First()
                Return folder
            End If
        End If

        Return Nothing

    End Function

    Public Async Function GetRootFolderAsync() As Task(Of Windows.Storage.StorageFolder)

        Dim localSettings = Windows.Storage.ApplicationData.Current.LocalSettings

        Dim mruToken = localSettings.Values("RootFolder")

        If String.IsNullOrEmpty(mruToken) Then
            Return Nothing
        Else
            Try
                Return Await Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(mruToken)
            Catch ex As Exception
                Return Nothing
            End Try
        End If

    End Function

    Public Function GetRootFolder() As StorageFolder
        Return rootFolder
    End Function

    Public Async Function GetStorageFolderAsync(name As String) As Task(Of Windows.Storage.StorageFolder)

        If name.Equals("") Then
            Dim recipes = rootFolder
            Return recipes
        Else
            Dim folder = Await GetFolderAsync(name, False) 'Do not read folder content
            If folder IsNot Nothing Then
                Return folder.Folder
            Else
                Return Nothing
            End If
        End If
    End Function

    Public Async Function GetRecipeAsync(mainCategory As String, category As String, title As String) As Task(Of Recipe)

        Dim folder = Await GetFolderAsync(mainCategory, ReadContent:=False)
        If folder IsNot Nothing Then
            Return Await folder.GetRecipeAsync(category, title)
        End If
        Return Nothing

    End Function

    Public Async Function LoadAsync(Optional setEditMode = False) As Task

        rootFolder = Await GetRootFolderAsync()
        If rootFolder Is Nothing Then
            Return
        End If

        Dim images As StorageFolder
        Dim templates As StorageFolder
        Dim help As StorageFolder
        Dim folders As IReadOnlyList(Of StorageFolder)

        Try
            images = Await rootFolder.GetFolderAsync("_folders")
        Catch ex As Exception
            App.Logger.Write("Unable to open the image folder:" + ex.ToString)
        End Try

        If Not initialized Then
            If images Is Nothing Then
                Try
                    images = Await rootFolder.CreateFolderAsync("_folders")
                Catch ex As Exception
                    App.Logger.Write("Unable to create the image folder:" + ex.ToString)
                End Try
            End If

            Try
                templates = Await rootFolder.GetFolderAsync(RecipeTemplates.FolderName)
            Catch ex As Exception
            End Try
            If templates Is Nothing Then
                templates = Await RecipeTemplates.CreateTemplateFolder(images)
            End If

            Try
                help = Await rootFolder.GetFolderAsync(HelpDocuments.FolderName)
            Catch ex As Exception
            End Try
            If help Is Nothing Then
                help = Await HelpDocuments.CreateHelpFolder()
            Else
                Await HelpDocuments.UpdateHelpFolder(help)
            End If

            ' Remove all temporary files
            Try
                Dim tempFolder = Windows.Storage.ApplicationData.Current.LocalFolder
                Dim files = Await tempFolder.GetFilesAsync()
                For Each file In files
                    If file.Name.ToUpper.EndsWith(".PNG") Then
                        Await file.DeleteAsync()
                    End If
                Next
            Catch ex As Exception
                App.Logger.Write("An exception occurred while removing temporary files:" + ex.ToString)
            End Try

            _folders.Clear()
            _subFolders.Clear()
            _tagFolders.Clear()
        Else
            For Each folder In _folders
                folder.Active = False
            Next
            For Each folder In _subFolders
                folder.Active = False
            Next
            For Each folder In _tagFolders
                folder.Active = False
            Next
        End If

        Try
            folders = Await rootFolder.GetFoldersAsync()
        Catch ex As Exception
            App.Logger.Write("Unable to open the root folder:" + ex.ToString)
        End Try

        CategoryHierarchy.Clear()

        Dim templateFolder As RecipeFolder
        For Each folder In folders
            If Not folder.DisplayName.StartsWith("_") Then
                Dim category As RecipeFolder = Nothing
                If initialized Then
                    category = GetFolder(folder.DisplayName)
                End If
                If category Is Nothing Then
                    If folder.Name = HelpDocuments.FolderName Then
                        category = New HelpDocuments(folder)
                        HelpFolder = category
                    ElseIf folder.Name = RecipeTemplates.FolderName Then
                        category = New RecipeTemplates(folder)
                        templateFolder = category
                    Else
                        category = New RecipeFolder(folder)
                        _folders.Add(category)
                    End If

                    If Not folder.Name.Equals(RecipeTemplates.FolderName) Then
                        category.EditModeActive = setEditMode
                    End If

                    If images IsNot Nothing Then
                        Try
                            Dim categoryImage As StorageFile = Nothing
                            Try
                                categoryImage = Await images.GetFileAsync(folder.DisplayName + ".png")
                            Catch ex As Exception
                            End Try
                            If categoryImage Is Nothing Then
                                Try
                                    categoryImage = Await images.GetFileAsync(folder.DisplayName + ".jpg")
                                Catch ex As Exception
                                End Try
                            End If
                            If categoryImage IsNot Nothing Then
                                ' Open a stream for the selected file.
                                Dim fileStream = Await categoryImage.OpenAsync(Windows.Storage.FileAccessMode.Read)
                                ' Set the image source to the selected bitmap.
                                category.Image = New Windows.UI.Xaml.Media.Imaging.BitmapImage()
                                Await category.Image.SetSourceAsync(fileStream)
                                category.ImageFile = categoryImage
                            End If
                        Catch ex As Exception
                            App.Logger.Write("Error occurred while loading image for " + folder.DisplayName + ": " + ex.ToString)
                        End Try
                    End If
                End If
                category.Active = True
                If category.Name <> RecipeTemplates.FolderName AndAlso category.Name <> HelpDocuments.FolderName Then
                    Dim hierarchyEntry As New FolderDescriptor
                    hierarchyEntry.CategoryPath = category.Name
                    hierarchyEntry.CategoryWithIndent = category.Name
                    CategoryHierarchy.Add(hierarchyEntry)
                    Await ReadSubCategoriesAsync(category, 1)
                End If
            End If
        Next

        If templateFolder IsNot Nothing Then
            _folders.Add(templateFolder)
        End If

        _folders.Add(New PlusFolder() With {.Active = True})

        For Each tag In TagRepository.Current.Directory
            Dim tagFolderInstance As TagFolder = Nothing
            If initialized Then
                tagFolderInstance = GetFolder(tag.Tag)
            End If
            If tagFolderInstance Is Nothing Then
                tagFolderInstance = New TagFolder(tag.Tag)
                tagFolderInstance.EditModeActive = setEditMode
                If images IsNot Nothing Then
                    Try
                        Dim tagImage As StorageFile = Nothing
                        Try
                            tagImage = Await images.GetFileAsync(tag.Tag + ".png")
                        Catch ex As Exception
                        End Try
                        If tagImage Is Nothing Then
                            Try
                                tagImage = Await images.GetFileAsync(tag.Tag + ".jpg")
                            Catch ex As Exception
                            End Try
                        End If
                        If tagImage IsNot Nothing Then
                            ' Open a stream for the selected file.
                            Dim fileStream = Await tagImage.OpenAsync(Windows.Storage.FileAccessMode.Read)
                            ' Set the image source to the selected bitmap.
                            tagFolderInstance.Image = New Windows.UI.Xaml.Media.Imaging.BitmapImage()
                            Await tagFolderInstance.Image.SetSourceAsync(fileStream)
                            tagFolderInstance.ImageFile = tagImage
                        End If
                    Catch ex As Exception
                        App.Logger.Write("Error occurred while loading image for " + tag.Tag + ": " + ex.ToString)
                    End Try
                End If

                _tagFolders.Add(tagFolderInstance)
            End If
            tagFolderInstance.Active = True
        Next
        _tagFolders.Add(New PlusFolder() With {.Active = True})
        If initialized Then
            ' Remove folders from the collection that no longer exist
            Dim inactiveFolders As New List(Of Integer)
            Dim index As Integer = 0
            For Each folder In _folders
                If Not folder.Active Then
                    inactiveFolders.Insert(0, index)
                End If
                index = index + 1
            Next
            For Each i In inactiveFolders
                _folders.RemoveAt(i)
            Next
            inactiveFolders.Clear()
            index = 0
            For Each folder In _subFolders
                If Not folder.Active Then
                    inactiveFolders.Insert(0, index)
                End If
                index = index + 1
            Next
            For Each i In inactiveFolders
                _subFolders.RemoveAt(i)
            Next
            inactiveFolders.Clear()
            index = 0
            For Each folder In _tagFolders
                If Not folder.Active Then
                    inactiveFolders.Insert(0, index)
                End If
                index = index + 1
            Next
            For Each i In inactiveFolders
                _tagFolders.RemoveAt(i)
            Next
        Else
            FavoriteFolder = New Favorites
            SearchResultsFolder = New SearchResults
            HistoryFolder = New History
            Await HistoryFolder.InitAsync()
        End If

        initialized = True
    End Function

    Private Async Function ReadSubCategoriesAsync(folder As RecipeFolder, Level As Integer) As Task
        Try
            Dim folders = Await folder.Folder.GetFoldersAsync()
            If folders IsNot Nothing Then
                Dim categoryPath As String
                Dim subfolder As RecipeFolder
                For Each item In folders
                    categoryPath = folder.Name + "\" + item.DisplayName
                    subfolder = Nothing
                    If initialized Then
                        subfolder = GetFolder(categoryPath)
                    End If
                    If subfolder Is Nothing Then
                        subfolder = New RecipeFolder(folder, categoryPath, item)
                        _subFolders.Add(subfolder)
                    End If
                    subfolder.Active = True
                    Dim hierarchyEntry As New FolderDescriptor
                    hierarchyEntry.CategoryPath = categoryPath
                    hierarchyEntry.CategoryWithIndent = item.DisplayName
                    For i = 1 To Level
                        hierarchyEntry.CategoryWithIndent = "    " + hierarchyEntry.CategoryWithIndent
                    Next
                    CategoryHierarchy.Add(hierarchyEntry)
                    Await ReadSubCategoriesAsync(subfolder, Level + 1)
                Next
            End If
        Catch ex As Exception
        End Try
    End Function

    Public Async Function UpdateMetadataAsync(changedRecipe As Recipe) As Task

        Dim folder = GetFolder(changedRecipe.Category)

        Await RecipeMetadata.Instance.WriteMetadataAsync(folder.Folder, changedRecipe)

        folder.UpdateStatistics(changedRecipe)

        SearchResultsFolder.UpdateStatistics(changedRecipe)
        FavoriteFolder.UpdateStatistics(changedRecipe)

    End Function


    Public Sub UpdateNote(changedRecipe As Recipe)

        Dim folder = GetFolder(changedRecipe.Category)

        folder.UpdateNote(changedRecipe)

        SearchResultsFolder.UpdateNote(changedRecipe)
        FavoriteFolder.UpdateNote(changedRecipe)

    End Sub

    Async Function ChangeRootFolder() As Task

        Dim localSettings = Windows.Storage.ApplicationData.Current.LocalSettings

        Dim mruToken = localSettings.Values("RootFolder")
        Dim folder As Windows.Storage.StorageFolder

        Dim openPicker = New Windows.Storage.Pickers.FolderPicker()
        openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        openPicker.CommitButtonText = App.Texts.GetString("OpenRootFolder")
        openPicker.FileTypeFilter.Clear()
        openPicker.FileTypeFilter.Add("*")
        openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail
        folder = Await openPicker.PickSingleFolderAsync()
        If folder IsNot Nothing Then
            ' Add picked file to MostRecentlyUsedList.
            mruToken = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Add(folder)
            localSettings.Values("RootFolder") = mruToken
        Else
            Return
        End If

        If HistoryFolder IsNot Nothing Then
            Await HistoryFolder.CreateBackupAndCloseAsync()
        End If

        Await MetaDataDatabase.Current.CreateBackupAndCloseAsync()

        initialized = False
        Await LoadAsync()

        If initialized Then
            SearchResultsFolder.Clear()
            Await HistoryFolder.TryRestoreBackupAsync()
            Await MetaDataDatabase.Current.TryRestoreBackupAsync()
        End If

    End Function

#Region "Delete Recipe"
    Async Function DeleteRecipeAsync(recipe As Recipe) As Task

        Dim failed As Boolean

        Try
            If recipe.File IsNot Nothing Then
                Await recipe.File.DeleteAsync()
            ElseIf recipe.RecipeSource IsNot Nothing Then
                Await recipe.RecipeSource.DeleteAsync()
            Else
                failed = True
            End If
        Catch ex As Exception
            App.Logger.Write("DeleteAsync failed: " + ex.Message)
            failed = True
        End Try

        If failed Then
            Dim dialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("UnableToDelete"))
            Await dialog.ShowAsync()
            Return
        End If

        Dim folder = GetFolder(recipe.Category)

        Try
            If recipe.Notes IsNot Nothing Then
                Await recipe.Notes.DeleteAsync()
            End If
        Catch ex As Exception
        End Try

        Try
            Dim metadata = Await folder.Folder.GetFileAsync(recipe.Name + ".xml")
            Await metadata.DeleteAsync()
        Catch ex As Exception
            App.Logger.WriteException("Delete xml failed:", ex)
        End Try

        Dim files As IReadOnlyList(Of StorageFile) = Await folder.GetImageFilesOfRecipeAsync(recipe)
        recipe.Pictures = Nothing
        For Each file In files
            Try
                Await file.DeleteAsync()
            Catch ex As Exception
                App.Logger.WriteException("Delete image failed:", ex)
            End Try
        Next


        folder.DeleteRecipe(recipe)
        If Not FavoriteFolder.ContentLoaded Then
            Await FavoriteFolder.LoadAsync()
        End If
        FavoriteFolder.DeleteRecipe(recipe)
        SearchResultsFolder.DeleteRecipe(recipe)
    End Function
#End Region

#Region "DeleteFolder"
    Public Async Function DeleteFolder(toDelete As RecipeFolder) As Task
        Await toDelete.DeleteFolder()
        If toDelete.IsLabelFolder() Then
            TagFolders.Remove(toDelete)
        Else
            Folders.Remove(toDelete)
        End If
    End Function
#End Region

#Region "Categories"

    Public Async Function ChangeCategoryAsync(ByVal recipeToChange As Recipe, ByVal destinationCategory As RecipeFolder) As Task

        If recipeToChange.Category = destinationCategory.Name Then
            Return
        End If

        Dim oldCategory As String = recipeToChange.Category

        If recipeToChange.ItemType = Recipe.ItemTypes.Recipe Then

            Dim errorFlag As Boolean
            Dim srcFolder As RecipeFolder

            Try
                If recipeToChange.File IsNot Nothing Then
                    Await recipeToChange.File.MoveAsync(destinationCategory.Folder, recipeToChange.File.Name, Windows.Storage.NameCollisionOption.GenerateUniqueName)
                ElseIf recipeToChange.RecipeSource IsNot Nothing Then
                    Await recipeToChange.RecipeSource.MoveAsync(destinationCategory.Folder, recipeToChange.RecipeSource.Name, Windows.Storage.NameCollisionOption.GenerateUniqueName)
                Else
                    errorFlag = True
                    Exit Try
                End If

                If destinationCategory.ContentLoaded Then
                    destinationCategory.Invalidate()
                End If
                srcFolder = GetFolder(recipeToChange.Category)
                If srcFolder.ContentLoaded Then
                    srcFolder.DeleteRecipe(recipeToChange)
                End If

            Catch ex As Exception
                App.Logger.Write("MoveAsync failed: " + ex.Message + "(" + ex.ToString() + ")")
                errorFlag = True
            End Try

            If errorFlag Or srcFolder Is Nothing Then
                Dim messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("UnableToEditCategory"))
                Await messageDialog.ShowAsync()
                Return
            End If

            ' Move notes
            Try
                If recipeToChange.Notes IsNot Nothing Then
                    Await recipeToChange.Notes.MoveAsync(destinationCategory.Folder, recipeToChange.Notes.Name, Windows.Storage.NameCollisionOption.GenerateUniqueName)
                End If
            Catch ex As Exception
            End Try


            'Move metadata if they exist
            Try
                Dim item As StorageFile = Await srcFolder.Folder.TryGetItemAsync(recipeToChange.Name + ".xml")
                If item IsNot Nothing Then
                    Dim metadataFile As Windows.Storage.StorageFile = TryCast(item, Windows.Storage.StorageFile)
                    If metadataFile IsNot Nothing Then
                        Await metadataFile.MoveAsync(destinationCategory.Folder, metadataFile.Name, Windows.Storage.NameCollisionOption.GenerateUniqueName)
                    End If
                End If
            Catch ex As Exception
            End Try

            'Move images
            Dim files As IReadOnlyList(Of StorageFile) = Await srcFolder.GetImageFilesOfRecipeAsync(recipeToChange)
            recipeToChange.Pictures = Nothing
            For Each file In files
                Try
                    Await file.MoveAsync(destinationCategory.Folder, file.Name, Windows.Storage.NameCollisionOption.GenerateUniqueName)
                Catch ex As Exception
                    App.Logger.WriteException("Move image failed:", ex)
                End Try
            Next

            'Search results
            SearchResultsFolder.ChangeCategory(recipeToChange, oldCategory, destinationCategory.Name)

            'Tags
            MetaDataDatabase.Current.ChangeCategory(recipeToChange, oldCategory, destinationCategory.Name)
            recipeToChange.LoadTags()
            For Each tag In recipeToChange.Tags
                Dim f = GetFolder(tag.Tag)
                If f IsNot Nothing AndAlso f.ContentLoaded Then
                    Await f.LoadAsync()
                End If
            Next
        End If
        FavoriteFolder.ChangeCategory(recipeToChange, oldCategory, destinationCategory.Name)
        HistoryFolder.ChangeCategory(recipeToChange, oldCategory, destinationCategory.Name)

        recipeToChange.Category = destinationCategory.Name
        recipeToChange.RenderSubTitle()

    End Function

    Private Function GetExtension(aFile As StorageFile) As String
        Dim pos = aFile.Name.LastIndexOf(".")
        If pos > 0 Then '0123.jpg  : length = 8, pos = 4
            Return aFile.Name.Substring(pos)
        End If
        Return ""
    End Function

    Public Async Function ModifyCategoryAsync(ByVal originalCategory As RecipeFolder, ByVal newCategoryName As String, ByVal categoryImageFile As Windows.Storage.StorageFile) As Task

        Dim errorFlag As Boolean
        Dim reload As Boolean
        Dim newFolderpath As String

        Try
            If Not originalCategory.Name.Equals(newCategoryName) Then
                Await originalCategory.Folder.RenameAsync(newCategoryName)
                Dim pos As Integer = originalCategory.Name.LastIndexOf("\")
                If pos >= 0 Then ' Brunch\Eierspeisen => pos = 6
                    newFolderpath = originalCategory.Name.Substring(0, pos + 1) + newCategoryName
                Else
                    newFolderpath = newCategoryName
                End If
                FavoriteFolder.RenameCategory(originalCategory.Name, newFolderpath)
                HistoryFolder.RenameCategory(originalCategory.Name, newFolderpath)
                MetaDataDatabase.Current.RenameCategory(originalCategory.Name, newCategoryName)
                reload = True
            End If

            If categoryImageFile IsNot Nothing Then
                If originalCategory.ImageFile IsNot Nothing AndAlso originalCategory.ImageFile.Path.Equals(categoryImageFile.Path) Then
                    Await categoryImageFile.RenameAsync(newCategoryName + GetExtension(categoryImageFile))
                Else
                    Await CopyCategoryImage(newCategoryName, categoryImageFile)
                End If
                reload = True
            End If

            If reload Then
                Await LoadAsync(setEditMode:=True)
            End If
        Catch ex As Exception
            errorFlag = True
        End Try

        If errorFlag Then
            Dim messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("UnableToEditCategory"))
            Await messageDialog.ShowAsync()
        End If

    End Function

    Private Async Function CopyCategoryImage(ByVal newCategoryName As String, ByVal categoryImageFile As Windows.Storage.StorageFile) As Task

        If categoryImageFile IsNot Nothing Then
            Dim images As Windows.Storage.StorageFolder
            Try
                images = Await rootFolder.GetFolderAsync("_folders")
            Catch ex As Exception
            End Try
            If images Is Nothing Then
                images = Await rootFolder.CreateFolderAsync("_folders")
            End If

            Await categoryImageFile.CopyAsync(images, newCategoryName + GetExtension(categoryImageFile), Windows.Storage.NameCollisionOption.ReplaceExisting)
        End If

    End Function

    Public Async Function CreateCategoryAsync(ByVal newCategoryName As String, ByVal categoryImageFile As Windows.Storage.StorageFile) As Task

        Dim errorFlag As Boolean

        Try
            Await rootFolder.CreateFolderAsync(newCategoryName)

            Await CopyCategoryImage(newCategoryName, categoryImageFile)

            Await LoadAsync()
        Catch ex As Exception
            errorFlag = True
        End Try

        If errorFlag Then
            Dim messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("UnableToCreateCategory"))
            Await messageDialog.ShowAsync()
        End If

    End Function

#End Region

#Region "Tags"

    Public Async Function CreateTagAsync(ByVal newTag As TagDirectory, ByVal tagImageFile As Windows.Storage.StorageFile) As Task

        Dim errorFlag As Boolean

        Try
            TagRepository.Current.AddTag(newTag.Tag, newTag.GetBackground(), newTag.GetForeground())
            Await CopyCategoryImage(newTag.Tag, tagImageFile)

            Await LoadAsync()
        Catch ex As Exception
            errorFlag = True
        End Try

        If errorFlag Then
            Dim messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("UnableToCreateTag"))
            Await messageDialog.ShowAsync()
        End If

    End Function

    Public Async Function ModifyTagAsync(ByVal folder As RecipeFolder, ByVal newTag As TagDirectory, ByVal tagImageFile As Windows.Storage.StorageFile) As Task

        Dim errorFlag As Boolean
        Dim reload As Boolean

        Try
            folder.Background = newTag.GetBackground()
            folder.Foreground = newTag.GetForeground()

            If Not TagRepository.Current.TagsAreEqual(folder.Name, newTag) Then
                TagRepository.Current.RenameTag(folder.Name, newTag)
                reload = True
            End If

            If tagImageFile IsNot Nothing Then
                If folder.ImageFile IsNot Nothing AndAlso folder.ImageFile.Path.Equals(tagImageFile.Path) Then
                    If Not folder.Name.Equals(newTag.Tag) Then
                        Await tagImageFile.RenameAsync(newTag.Tag + GetExtension(tagImageFile))
                    End If
                Else
                    Await CopyCategoryImage(newTag.Tag, tagImageFile)
                    reload = True
                End If
            End If

            If reload Then
                initialized = False
                Await LoadAsync(setEditMode:=True)
            End If
        Catch ex As Exception
            errorFlag = True
        End Try

        If errorFlag Then
            Dim messageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("UnableToEditTag"))
            Await messageDialog.ShowAsync()
        End If

    End Function

#End Region

#Region "RenameRecipe"

    Public Async Function RenameRecipeAsync(recipeToRename As Recipe, newName As String) As Task

        Dim oldName As New String(recipeToRename.Name.ToCharArray) ' Create a real copy
        Dim folder = GetFolder(recipeToRename.Category)

        'Rename XML file
        If recipeToRename.CookedNoOfTimes > 0 AndAlso recipeToRename.ItemType = Recipe.ItemTypes.Recipe Then
            Try
                Dim xmlFile As StorageFile
                xmlFile = Await folder.Folder.GetFileAsync(oldName + ".xml")
                If xmlFile IsNot Nothing Then
                    Await xmlFile.RenameAsync(newName + ".xml")
                End If
            Catch ex As Exception
                App.Logger.Write("RenameAsync failed (XML File): " + ex.Message)
            End Try
        End If

        'Rename notes
        If recipeToRename.Notes IsNot Nothing Then
            Try
                Await recipeToRename.Notes.RenameAsync(newName + ".rtf")
            Catch ex As Exception
                App.Logger.Write("RenameAsync failed (RTF Notes File): " + ex.Message)
            End Try
        End If

        'Rename recipe source file
        If recipeToRename.RecipeSource IsNot Nothing Then
            Try
                Await recipeToRename.RecipeSource.RenameAsync(newName + ".rtf")
            Catch ex As Exception
                App.Logger.Write("RenameAsync failed (RTF Source File): " + ex.Message)
            End Try
        End If

        'Rename images
        Dim files As IReadOnlyList(Of StorageFile) = Await folder.GetImageFilesOfRecipeAsync(recipeToRename)
        If files IsNot Nothing Then
            For Each bitmapFile In files
                Try
                    Dim bitmapSuffix As String = bitmapFile.Name.Substring(bitmapFile.Name.IndexOf("_image_"))
                    Await bitmapFile.RenameAsync(newName + bitmapSuffix)
                Catch ex As Exception
                    App.Logger.Write("RenameAsync failed (Bitmap): " + ex.Message)
                End Try
            Next
        End If

        'Rename recipe file if it is not an external recipe
        Try
            If recipeToRename.ItemType = Recipe.ItemTypes.Recipe AndAlso recipeToRename.File IsNot Nothing Then
                Await recipeToRename.File.RenameAsync(newName + ".pdf")
            End If
        Catch ex As Exception
            App.Logger.Write("RenameAsync failed (PDF): " + ex.Message)
            Return
        End Try

        'Update tag database
        MetaDataDatabase.Current.RenameRecipe(recipeToRename, oldName, newName)

        'Rename was successful so far. Update references.
        folder.RenameRecipe(recipeToRename, oldName, newName)

        FavoriteFolder.RenameRecipe(recipeToRename, oldName, newName)
        SearchResultsFolder.RenameRecipe(recipeToRename, oldName, newName)
        HistoryFolder.RenameRecipe(recipeToRename, oldName, newName)

    End Function

    Public Async Function DeleteFolderAsync(toDelete As RecipeFolder) As Task

        Dim deleted As Boolean = False

        Try
            Await toDelete.Folder.DeleteAsync()
            deleted = True
        Catch ex As Exception
        End Try

        If Not deleted Then
            Dim msg = New MessageDialog(App.Texts.GetString("CannotDeleteFolder"))
            Await msg.ShowAsync()
            Return
        End If

        _subFolders.Remove(toDelete)

        Dim matches = CategoryHierarchy.Where(Function(otherFolder) otherFolder.CategoryPath.Equals(toDelete.Name))
        If matches.Count() = 1 Then
            Dim folder = matches.First()
            CategoryHierarchy.Remove(folder)
        End If

        Dim entry As Recipe = toDelete.Parent.GetSubcategory(toDelete.Parent.Name, toDelete.DisplayName)
        If entry IsNot Nothing Then
            toDelete.Parent.Recipes.Remove(entry)
        End If
    End Function

#End Region

#Region "Favorites"
    Public Sub SetIsFavorite(recipe As Recipe)
        Dim folder = GetFolder(recipe.Category)

        folder.SetIsFavorite(recipe)

        SearchResultsFolder.SetIsFavorite(recipe)
        FavoriteFolder.SetIsFavorite(recipe)
        HistoryFolder.SetIsFavorite(recipe)
    End Sub
#End Region
End Class

Imports RecipeBrowser.Recipe
Imports Windows.Storage
Imports Windows.Storage.Search
Imports Windows.UI

Public Class RecipeFolder
    Implements INotifyPropertyChanged

    Public Sub New(aName As String)
        Name = aName
        DisplayName = Name

        _favoritesVisibility = Visibility.Visible
        _historyVisibility = Visibility.Visible
        _refreshVisibility = Visibility.Visible
        _scanForCaloriesVisibility = Visibility.Visible
        _editTagsVisibility = Visibility.Visible
        _recipeSearchBoxVisibility = Visibility.Visible
        _recipsSearchBoxEnabled = True
        _changeCategoryVisibility = Visibility.Visible
        _deleteRecipeVisibility = Visibility.Visible
        _renameRecipeVisibility = Visibility.Visible
        _editNoteVisibility = Visibility.Visible
        _editFileVisibility = Visibility.Visible
        _logAsCookedVisibility = Visibility.Visible
        _shareVisibility = Visibility.Visible
        _setFilterVisibility = Visibility.Collapsed
        _showImageGalleryVisibility = Visibility.Visible
        _addExternalRecipeVisibility = Visibility.Collapsed
        _addRecipeVisibility = Visibility.Visible
        _exportImportVisibility = Visibility.Collapsed
        _changeCalories = Visibility.Visible
        _changeSortOrderVisibility = Visibility.Visible
        _openFileVisibility = Visibility.Visible
        _exportCaloricInfosVisibility = Visibility.Visible
        _helpVisibility = Visibility.Visible

        _searchBoxSpaceHolderWidth = 0
    End Sub

    Public Sub New(aFolder As StorageFolder)
        Folder = aFolder
        Name = aFolder.DisplayName
        DisplayName = aFolder.DisplayName
    End Sub

    Public Sub New(parentFolder As RecipeFolder, aFolderPath As String, aFolder As StorageFolder)
        Folder = aFolder
        Name = aFolderPath
        DisplayName = aFolder.DisplayName
        Parent = parentFolder
    End Sub

    Public Overridable Function IsSpecialFolder() As Boolean
        Return Name = HelpDocuments.FolderName OrElse Name = RecipeTemplates.FolderName
    End Function

    Public Overridable Function IsLabelFolder() As Boolean
        Return False
    End Function

#Region "Properties"
    Protected Overridable Sub OnPropertyChanged(ByVal PropertyName As String)
        ' Raise the event, and make this procedure
        ' overridable, should someone want to inherit from
        ' this class and override this behavior:
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(PropertyName))
    End Sub

    Public Property Name As String
    Public Property DisplayName As String
    Public Property Folder As Windows.Storage.StorageFolder
    Public Property Parent As RecipeFolder
    Public Property Image As BitmapImage
    Public Property ImageFile As Windows.Storage.StorageFile
    Public Property Active As Boolean
    Public Property Background As Color
    Public Property Foreground As Color
    Public Property IsPlus As Boolean = False

    Private _editModeActive As Boolean = False
    Public Property EditModeActive As Boolean
        Get
            Return _editModeActive
        End Get
        Set(value As Boolean)
            If value <> _editModeActive Then
                _editModeActive = value
                OnPropertyChanged("EditModeActive")
            End If
        End Set
    End Property

    Private _loadProgressTotal As Double = 1
    Public Property LoadProgressTotal As Double
        Get
            Return _loadProgressTotal
        End Get
        Set(value As Double)
            If value <> _loadProgressTotal Then
                _loadProgressTotal = value
                OnPropertyChanged("LoadProgressTotal")
            End If
        End Set
    End Property

    Private _loadProgressValue As Double = 0
    Public Property LoadProgressValue As Double
        Get
            Return _loadProgressValue
        End Get
        Set(value As Double)
            If value <> _loadProgressValue Then
                _loadProgressValue = value
                OnPropertyChanged("LoadProgressValue")
            End If
        End Set
    End Property

    Public Enum SortOrder
        ByNameAscending
        ByDateDescending
        ByLastCookedDescending
        NoSorting
    End Enum

    Protected _Recipes As New ObservableCollection(Of Recipe)()

    Public ReadOnly Property Recipes As ObservableCollection(Of Recipe)
        Get
            Return _Recipes
        End Get
    End Property

    Protected _SubCategories As New List(Of Recipe)()
    Public ReadOnly Property SubCategories As List(Of Recipe)
        Get
            Return _SubCategories
        End Get
    End Property

    Protected _GroupedRecipes As New ObservableCollection(Of RecipesGroup)()
    Public ReadOnly Property GroupedRecipes As ObservableCollection(Of RecipesGroup)
        Get
            Return _GroupedRecipes
        End Get
    End Property

    Protected _ContentLoaded As Boolean
    Public Property ContentIsGrouped As Boolean
    Public Function ContentLoaded() As Boolean
        Return _ContentLoaded
    End Function
#End Region

#Region "Visibility_Properties"

    Protected _favoritesVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property FavoritesVisibility As Visibility
        Get
            Return _favoritesVisibility
        End Get
    End Property

    Protected _historyVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property HistoryVisibility As Visibility
        Get
            Return _historyVisibility
        End Get
    End Property

    Protected _refreshVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property RefreshVisibility As Visibility
        Get
            Return _refreshVisibility
        End Get
    End Property

    Protected _scanForCaloriesVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property ScanForCaloriesVisibility As Visibility
        Get
            Return _scanForCaloriesVisibility
        End Get
    End Property

    Protected _editTagsVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property EditTagsVisibility As Visibility
        Get
            Return _editTagsVisibility
        End Get
    End Property

    Protected _recipsSearchBoxEnabled As Boolean = True
    Public ReadOnly Property RecipeSearchBoxEnabled As Boolean
        Get
            Return _recipsSearchBoxEnabled
        End Get
    End Property

    Protected _recipeSearchBoxVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property RecipeSearchboxVisibility As Visibility
        Get
            Return _recipeSearchBoxVisibility
        End Get
    End Property

    Protected _changeCategoryVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property ChangeCategoryVisibility As Visibility
        Get
            Return _changeCategoryVisibility
        End Get
    End Property

    Protected _deleteRecipeVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property DeleteRecipeVisibility As Visibility
        Get
            Return _deleteRecipeVisibility
        End Get
    End Property

    Protected _renameRecipeVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property RenameRecipeVisibility As Visibility
        Get
            Return _renameRecipeVisibility
        End Get
    End Property

    Protected _editNoteVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property EditNoteVisibility As Visibility
        Get
            Return _editNoteVisibility
        End Get
    End Property

    Protected _editFileVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property EditFileVisibility As Visibility
        Get
            Return _editFileVisibility
        End Get
    End Property

    Protected _logAsCookedVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property LogAsCookedVisibility As Visibility
        Get
            Return _logAsCookedVisibility
        End Get
    End Property

    Protected _shareVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property ShareVisibility As Visibility
        Get
            Return _shareVisibility
        End Get
    End Property

    Protected _setFilterVisibility As Visibility = Visibility.Collapsed
    Public ReadOnly Property SetFilterVisibility As Visibility
        Get
            Return _setFilterVisibility
        End Get
    End Property

    Protected _changeSortOrderVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property ChangeSortOrderVisibility As Visibility
        Get
            Return _changeSortOrderVisibility
        End Get
    End Property

    Protected _showImageGalleryVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property ShowImageGalleryVisibility As Visibility
        Get
            Return _showImageGalleryVisibility
        End Get
    End Property

    Protected _addExternalRecipeVisibility As Visibility = Visibility.Collapsed
    Public ReadOnly Property AddExternalRecipeVisibility As Visibility
        Get
            Return _addExternalRecipeVisibility
        End Get
    End Property

    Protected _addRecipeVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property AddRecipeVisibility As Visibility
        Get
            Return _addRecipeVisibility
        End Get
    End Property

    Protected _exportImportVisibility As Visibility = Visibility.Collapsed
    Public ReadOnly Property ExportImportVisibility As Visibility
        Get
            Return _exportImportVisibility
        End Get
    End Property

    Protected _openFileVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property OpenFileVisibility As Visibility
        Get
            Return _openFileVisibility
        End Get
    End Property

    Protected _changeCalories As Visibility = Visibility.Visible
    Public ReadOnly Property ChangeCaloriesVisibility As Visibility
        Get
            Return _changeCalories
        End Get
    End Property

    Protected _exportCaloricInfosVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property ExportCaloricInfosVisibility As Visibility
        Get
            Return _exportCaloricInfosVisibility
        End Get
    End Property

    Protected _helpVisibility As Visibility = Visibility.Visible
    Public ReadOnly Property HelpVisibility As Visibility
        Get
            Return _helpVisibility
        End Get
    End Property

    Protected _searchBoxSpaceHolderWidth As Integer = 0
    Public ReadOnly Property SearchBoxSpaceHolderWidth As Integer
        Get
            Return _searchBoxSpaceHolderWidth
        End Get
    End Property
#End Region

#Region "Sorting"
    Protected _SortOrder As SortOrder = SortOrder.ByNameAscending
    Protected _RecipeList As New List(Of Recipe)
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private Function GetOrCreateGroup(title As String) As RecipesGroup

        Dim matches = GroupedRecipes.Where(Function(otherGroup) otherGroup.Key.Equals(title))
        If matches.Count() > 0 Then
            Return matches.First()
        Else
            Dim newGroup As New RecipesGroup() With {.Key = title}
            GroupedRecipes.Add(newGroup)
            Return newGroup
        End If
        Return Nothing

    End Function

    Private Sub CreateCategoryGroups()
        GroupedRecipes.Clear()

        For Each r In _RecipeList
            Dim g = GetOrCreateGroup(r.DisplayCategory)
            g.Add(r)
        Next
    End Sub

    Protected Overridable Sub ApplySortOrder()
        Dim _comparer As IComparer(Of Recipe)

        ' Sort recipes according selected sort order
        Select Case _SortOrder
            Case SortOrder.ByNameAscending
                _comparer = New RecipeComparer_NameAscending
            Case SortOrder.ByDateDescending
                _comparer = New RecipeComparer_DateDescending
            Case SortOrder.ByLastCookedDescending
                _comparer = New RecipeComparer_LastCookedDescending
        End Select
        If _comparer IsNot Nothing Then
            _RecipeList.Sort(_comparer)
        End If

        ' Sort subcategories according selected sort order
        _comparer = New RecipeComparer_NameAscending
        If _comparer IsNot Nothing Then
            _SubCategories.Sort(_comparer)
        End If

        ' Setup list
        _Recipes.Clear()
        For Each item In _SubCategories
            _Recipes.Add(item)
        Next
        For Each item In _RecipeList
            _Recipes.Add(item)
        Next

        If ContentIsGrouped Then
            CreateCategoryGroups()
        End If
    End Sub

    Public Sub SetSortOrder(ByVal order As SortOrder)
        If order <> _SortOrder Then
            _SortOrder = order
            ApplySortOrder()
        End If
    End Sub
#End Region

#Region "Load Recipe"
    Private Function GetFolder(ByRef file As Windows.Storage.StorageFile) As String

        ' Example for a path: Path = "G:\Users\Thomas\SkyDrive\Rezepte\Auflauf\Kohlrabi-Lasagne mit Spinat und Tomaten.pdf"

        Dim pos = file.Path.LastIndexOf("\")

        If pos = -1 Then
            Return file.Path
        End If

        Dim Folder = file.Path.Substring(0, pos)

        Return Folder

    End Function

    Private Function GetCategory(ByRef file As Windows.Storage.StorageFile) As String

        ' Example for a folder: Path = "G:\Users\Thomas\SkyDrive\Rezepte\Auflauf\Unterkategorie"

        Dim rootFolder = RecipeFolders.Current.GetRootFolder()

        Dim folderName As String = GetFolder(file)
        Dim category As String

        If folderName.Contains(rootFolder.Path) AndAlso Not folderName.Equals(rootFolder.Path) Then
            ' Should be the case!
            Return folderName.Substring(rootFolder.Path.Length + 1) ' Skip leading backslash
        End If

        Dim pos = folderName.LastIndexOf("\")
        If pos <> -1 Then
            category = folderName.Substring(pos + 1)
        Else
            category = folderName
        End If

        Return category

    End Function

    Async Function LoadRecipeAsync(file As Windows.Storage.StorageFile, loadMetadata As Boolean, checkForNotes As Boolean, Optional metaDataList As Persistency.RecipeMetaDataList = Nothing) As Task(Of Recipe)

        Dim _recipe = New Recipe
        Dim metaDataFile As Windows.Storage.StorageFile

        _recipe.Name = file.Name.Remove(file.Name.Length - 4)  ' delete suffix .pdf

        If Name = SearchResults.FolderName Then
            _recipe.Category = GetCategory(file)

            If _recipe.Category Is Nothing Then
                Return Nothing
            End If
            Try
                Dim parent As StorageFolder

                If checkForNotes Or loadMetadata Then
                    parent = RecipeFolders.GetInstance().GetFolder(_recipe.Category).Folder
                End If
                If checkForNotes And parent IsNot Nothing Then
                    _recipe.Notes = TryCast(Await parent.TryGetItemAsync(_recipe.Name + ".rtf"), Windows.Storage.StorageFile)
                End If
                If loadMetadata And parent IsNot Nothing Then
                    metaDataFile = TryCast(Await parent.TryGetItemAsync(_recipe.Name + ".xml"), Windows.Storage.StorageFile)
                End If
            Catch ex As Exception
                App.Logger.Write("Unable to access parent of: " + file.Path + ": " + ex.ToString)
            End Try
        Else
            _recipe.Category = Name
            Try
                If checkForNotes Then
                    _recipe.Notes = Await Folder.GetFileAsync(_recipe.Name + ".rtf")
                End If
            Catch ex As Exception
            End Try
            If loadMetadata Then
                Try
                    metaDataFile = Await Folder.GetFileAsync(_recipe.Name + ".xml")
                Catch ex As Exception
                End Try
            End If
        End If

        Dim properties = Await file.GetBasicPropertiesAsync()
        _recipe.CreationDateTime = properties.ItemDate.DateTime
        _recipe.File = file

        If metaDataFile IsNot Nothing Then
            Await RecipeMetadata.Instance.ReadMetadataAsync(_recipe, metaDataFile)
        End If

        If Name = HelpDocuments.FolderName Then
            _recipe.SubTitle = ""
        Else
            If metaDataList IsNot Nothing Then
                _recipe.Calories = metaDataList.GetMetadata(_recipe.Name).Calories
            Else
                _recipe.Calories = MetaDataDatabase.Current.GetCalories(_recipe.Category, _recipe.Name)
            End If
            '_recipe.RenderSubTitleTask = _recipe.RenderSubTitleAsync()
            _recipe.RenderSubTitle()
        End If

        Return _recipe

    End Function

    Async Function LoadRecipeFromSourceAsync(rtfFile As Windows.Storage.StorageFile, loadMetadata As Boolean, Optional metaDataList As Persistency.RecipeMetaDataList = Nothing) As Task(Of Recipe)

        Dim _recipe = New Recipe
        Dim metaDataFile As Windows.Storage.StorageFile

        _recipe.Name = rtfFile.Name.Remove(rtfFile.Name.Length - 4)  ' delete suffix .rtf

        If Name = SearchResults.FolderName Then
            _recipe.Category = GetCategory(rtfFile)

            If _recipe.Category Is Nothing Then
                Return Nothing
            End If
            Try
                Dim parent As StorageFolder

                If loadMetadata Then
                    parent = RecipeFolders.GetInstance().GetFolder(_recipe.Category).Folder
                End If
                If loadMetadata And parent IsNot Nothing Then
                    metaDataFile = TryCast(Await parent.TryGetItemAsync(_recipe.Name + ".xml"), Windows.Storage.StorageFile)
                End If
            Catch ex As Exception
                App.Logger.Write("Unable to access parent of: " + rtfFile.Path + ": " + ex.ToString)
            End Try
        Else
            _recipe.Category = Name
            If loadMetadata Then
                Try
                    metaDataFile = Await Folder.GetFileAsync(_recipe.Name + ".xml")
                Catch ex As Exception
                End Try
            End If
        End If

        Dim properties = Await rtfFile.GetBasicPropertiesAsync()
        _recipe.CreationDateTime = properties.ItemDate.DateTime
        _recipe.RecipeSource = rtfFile

        If metaDataFile IsNot Nothing Then
            Await RecipeMetadata.Instance.ReadMetadataAsync(_recipe, metaDataFile)
        End If

        If Name = HelpDocuments.FolderName Then
            _recipe.SubTitle = ""
        Else
            If metaDataList IsNot Nothing Then
                _recipe.Calories = metaDataList.GetMetadata(_recipe.Name).Calories
            Else
                _recipe.Calories = MetaDataDatabase.Current.GetCalories(_recipe.Category, _recipe.Name)
            End If
            _recipe.RenderSubTitle()
        End If

        Return _recipe

    End Function
#End Region

#Region "Load folder content"
    Protected Async Function SetUpFolderFromFileListAsync(fileList As IReadOnlyList(Of Windows.Storage.StorageFile), Optional skipResultsInHelp As Boolean = False, Optional tagFilter As String = "") As Task

        ' This method is used by the original folders and the search folder.
        _Recipes.Clear()
        _RecipeList.Clear()

        Try
            If fileList Is Nothing Then
                Return
            End If

            LoadProgressTotal = fileList.Count
            LoadProgressValue = 0

            Dim recipeMetadataList As Persistency.RecipeMetaDataList
            If Not IsSpecialFolder() Then
                recipeMetadataList = MetaDataDatabase.Current.GetMetadataForCategory(Name)
            End If

            Dim rtfFiles As New List(Of Windows.Storage.StorageFile)
            Dim xmlFiles As New List(Of Windows.Storage.StorageFile)

            For Each aFile In fileList
                LoadProgressValue = _loadProgressValue + 1
                If RecipesPage.Current IsNot Nothing AndAlso
                        RecipesPage.Current.CurrentRecipeFolder.Equals(Me) Then
                    RecipesPage.Current.SetLoadProgress(LoadProgressValue)
                End If
                Dim category = GetCategory(aFile)
                If skipResultsInHelp AndAlso
                (category.Equals(HelpDocuments.FolderName) OrElse category.Equals(RecipeTemplates.FolderName)) Then
                    Continue For ' Skip results in help documents and templates
                End If
                If aFile.Name.ToUpper.EndsWith(".PDF") Then
                    Try
                        Dim _recipe = Await LoadRecipeAsync(aFile, loadMetadata:=Name = SearchResults.FolderName, checkForNotes:=Name = SearchResults.FolderName, metaDataList:=recipeMetadataList)
                        If tagFilter.Length > 0 Then
                            _recipe.LoadTags()
                            If Not _recipe.HasTag(tagFilter) Then
                                Continue For
                            End If
                        End If
                        _RecipeList.Add(_recipe)
                    Catch ex As Exception
                        App.Logger.Write("Recipe cannot be loaded: " + aFile.Path + ex.ToString)
                    End Try
                ElseIf aFile.Name.ToUpper.EndsWith(".RTF") Then
                    rtfFiles.Add(aFile) ' Search folder: Add the recipe to the search result; Normal folder: Log the file in the recipe data
                ElseIf aFile.Name.ToUpper.EndsWith(".XML") Then
                    If Name <> SearchResults.FolderName Then
                        xmlFiles.Add(aFile) ' use the file instance later in order to load the metadata
                    End If
                Else
                    App.Logger.Write("Unsupported File: " + aFile.Path + aFile.ContentType)
                End If
            Next

            ' If the search expression has been found in a note file, try to add the corresponding recipe to the search result list
            For Each aFile In rtfFiles
                Dim recipeName = aFile.Name.Remove(aFile.Name.Length - 4)  ' delete suffix e.g. ".rtf"
                If Name = SearchResults.FolderName Then
                    Dim category = GetCategory(aFile)
                    If GetRecipe(category, recipeName) Is Nothing Then
                        Try ' Add the recipe to the search result
                            Dim parent = RecipeFolders.GetInstance().GetFolder(category).Folder
                            Dim recipeFile As StorageFile
                            Try
                                recipeFile = Await parent.GetFileAsync(recipeName + ".pdf")
                            Catch ex As Exception
                            End Try
                            Dim _recipe As Recipe
                            If recipeFile Is Nothing Then
                                _recipe = Await LoadRecipeFromSourceAsync(aFile, loadMetadata:=True)
                            Else
                                _recipe = Await LoadRecipeAsync(recipeFile, loadMetadata:=True, checkForNotes:=False, metaDataList:=recipeMetadataList)
                                _recipe.Notes = aFile
                            End If
                            If tagFilter.Length > 0 Then
                                _recipe.LoadTags()
                                If Not _recipe.HasTag(tagFilter) Then
                                    Continue For
                                End If
                            End If
                            _RecipeList.Add(_recipe)
                        Catch ex As Exception
                            App.Logger.Write("Unable to add recipe to search result: " + recipeName + ": " + ex.ToString)
                        End Try
                    End If
                Else
                    Dim _recipe As Recipe = GetRecipe(Name, recipeName)
                    If _recipe Is Nothing Then
                        ' Recipe that is represented by an RTF file rather than a PDF file
                        _recipe = Await LoadRecipeFromSourceAsync(aFile, True, metaDataList:=recipeMetadataList)
                        If tagFilter.Length > 0 Then
                            _recipe.LoadTags()
                            If Not _recipe.HasTag(tagFilter) Then
                                Continue For
                            End If
                        End If
                        _RecipeList.Add(_recipe)
                    Else
                        _recipe.Notes = aFile
                    End If
                End If
            Next

            For Each aFile In xmlFiles
                Dim recipeName = aFile.Name.Remove(aFile.Name.Length - 4)  ' delete suffix .xml
                Dim _recipe = GetRecipe(Name, recipeName)
                If _recipe IsNot Nothing Then
                    Await RecipeMetadata.Instance.ReadMetadataAsync(_recipe, aFile)
                End If
            Next

            ApplySortOrder()

            _ContentLoaded = True

        Catch ex As Exception
            App.Logger.WriteException("RecipeFolders.SetUpFolderFromFileListAsync", ex)
        End Try

    End Function


    Public Overridable Async Function LoadAsync() As Task

        _Recipes.Clear()
        _RecipeList.Clear()
        _SubCategories.Clear()

        Try
            ' Read folders
            Dim folders As IReadOnlyList(Of StorageFolder)
            Try
                folders = Await Folder.GetFoldersAsync()
            Catch ex As Exception
            End Try

            If folders IsNot Nothing Then
                For Each item In folders
                    _SubCategories.Add(New SubCategory(Me, item.Name))
                Next
            End If

            ' Read files
            Dim files As IReadOnlyList(Of StorageFile)

            Try
                files = Await Folder.GetFilesAsync()
            Catch ex As Exception
            End Try

            If files Is Nothing Then
                App.Logger.Write("Folder content cannot be read: " + Folder.DisplayName)
            ElseIf files.Count = 0 Then
                App.Logger.Write("Folder is empty: " + Folder.DisplayName)
            End If

            Await SetUpFolderFromFileListAsync(files)
        Catch ex As Exception
            App.Logger.WriteException("RecipeFolder.LoadAsync", ex)
        End Try

    End Function

    Sub Invalidate()

        _RecipeList.Clear()
        _SubCategories.Clear()
        _Recipes.Clear()
        _GroupedRecipes.Clear()
        _ContentLoaded = False

    End Sub

#End Region

#Region "Access recipes"
    Public Function GetRecipe(category As String, title As String) As Recipe

        Dim matches = _RecipeList.Where(Function(otherRecipe) otherRecipe.Name.Equals(title) And otherRecipe.Category.Equals(category))
        If matches.Count() > 0 Then
            Return matches.First()
        End If
        Return Nothing

    End Function

    Public Async Function GetRecipeAsync(category As String, title As String) As Task(Of Recipe)
        If ContentLoaded() Then
            Return GetRecipe(category, title)
        Else
            Dim file As Windows.Storage.StorageFile
            Try
                file = Await Folder.GetFileAsync(title + ".pdf")
            Catch ex As Exception
                Return Nothing
            End Try
            If file IsNot Nothing Then
                Return Await LoadRecipeAsync(file, loadMetadata:=True, checkForNotes:=True)
            End If
        End If

        Return Nothing
    End Function

    Public Function GetSubcategory(category As String, title As String) As Recipe

        Dim matches = _SubCategories.Where(Function(otherRecipe) otherRecipe.Name.Equals(title) And otherRecipe.Category.Equals(category))
        If matches.Count() > 0 Then
            Return matches.First()
        End If
        Return Nothing

    End Function

#End Region

#Region "Search recipes"
    Public Function GetMatchingRecipes(searchString As String) As IEnumerable(Of Recipe)

        Return Recipes.Where(Function(otherRecipe) otherRecipe.Name.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) > -1 And otherRecipe.ItemType <> ItemTypes.Header)

    End Function
#End Region

#Region "Delete recipe"
    Public Overridable Function DeleteRecipe(ByRef recipeToDelete As Recipe) As Boolean

        If Not ContentLoaded() Then
            Return False
        End If

        Dim deleted As Boolean = False

        If _RecipeList.Contains(recipeToDelete) Then
            _Recipes.Remove(recipeToDelete)
            _RecipeList.Remove(recipeToDelete)
            deleted = True
        Else
            Dim copy = GetRecipe(recipeToDelete.Category, recipeToDelete.Name)
            If copy IsNot Nothing Then
                deleted = DeleteRecipe(copy)
            End If
        End If
        If deleted AndAlso ContentIsGrouped Then
            CreateCategoryGroups()
        End If

        Return deleted

    End Function

#End Region

#Region "Delete folder"
    Public Overridable Async Function DeleteFolder() As Task
        If IsSpecialFolder() Then
            Return
        End If

        If Not ContentLoaded() Then
            Await LoadAsync()
            If _RecipeList.Count > 0 Or _SubCategories.Count > 0 Then
                Return
            End If
        End If

        Try
            Await Folder.DeleteAsync()
        Catch ex As Exception
        End Try
    End Function
#End Region

#Region "LastCooked"
    Public Sub UpdateStatistics(changedRecipe As Recipe)

        Dim recipe = GetRecipe(changedRecipe.Category, changedRecipe.Name)

        If recipe IsNot Nothing Then
            recipe.LastCooked = changedRecipe.LastCooked
            recipe.CookedNoOfTimes = changedRecipe.CookedNoOfTimes
            recipe.RenderSubTitle()
        End If

    End Sub

#End Region

#Region "Notes"
    Public Sub UpdateNote(changedRecipe As Recipe)

        Dim recipe = GetRecipe(changedRecipe.Category, changedRecipe.Name)

        If recipe IsNot Nothing AndAlso Not Object.ReferenceEquals(recipe, changedRecipe) Then
            recipe.Notes = changedRecipe.Notes
        End If
    End Sub

#End Region

#Region "Images"

    Private Async Function LoadImageAsync(ByVal imageFile As Windows.Storage.StorageFile) As Task(Of BitmapImage)

        If imageFile IsNot Nothing Then
            Try
                ' Open a stream for the selected file.
                Dim fileStream = Await imageFile.OpenAsync(Windows.Storage.FileAccessMode.Read)

                ' Set the image source to the selected bitmap.
                Dim bitmap = New Windows.UI.Xaml.Media.Imaging.BitmapImage()

                bitmap.SetSource(fileStream)
                Return bitmap
            Catch ex As Exception
            End Try
        End If

        Return Nothing
    End Function

    Public Async Function GetImageFilesOfRecipeAsync(aRecipe As Recipe) As Task(Of IReadOnlyList(Of StorageFile))


        If Folder Is Nothing Then
            Return Nothing
        End If

        Dim fileTypeFilter As New List(Of String)
        fileTypeFilter.Add(".jpg")
        fileTypeFilter.Add(".png")

        Dim queryOptions As New QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter)
        queryOptions.FolderDepth = FolderDepth.Shallow
        queryOptions.ApplicationSearchFilter = aRecipe.Name + "_image_*"

        Dim queryResult As StorageFileQueryResult = Folder.CreateFileQueryWithOptions(queryOptions)
        Dim files As IReadOnlyList(Of StorageFile) = Await queryResult.GetFilesAsync()

        Return files

    End Function

    Public Async Function GetImagesOfRecipeAsync(aRecipe As Recipe) As Task
        If Folder Is Nothing Then
            Return
        End If

        Dim files As IReadOnlyList(Of StorageFile) = Await GetImageFilesOfRecipeAsync(aRecipe)
        If files Is Nothing Then
            Return
        End If

        aRecipe.Pictures = New ObservableCollection(Of RecipeImage)
        For Each aFile In files
            Dim image = Await LoadImageAsync(aFile)
            If image IsNot Nothing Then
                aRecipe.Pictures.Add(New RecipeImage(aFile, image))
            End If
        Next
    End Function
#End Region

#Region "RenameRecipe"

    Public Overridable Sub RenameRecipe(ByRef recipeToRename As Recipe, ByVal oldName As String, ByVal newName As String)
        recipeToRename.Name = newName

        If ContentLoaded() AndAlso Not _RecipeList.Contains(recipeToRename) Then
            Dim copy = GetRecipe(recipeToRename.Category, oldName)
            If copy IsNot Nothing Then
                copy.Name = newName
                copy.File = recipeToRename.File
            End If
        End If

    End Sub

#End Region

#Region "ChangeCategory"

    Public Overridable Sub ChangeCategory(ByRef recipeToChange As Recipe, ByRef oldCategpry As String, ByRef newCategory As String)
        Dim changed As Boolean = False

        recipeToChange.Category = newCategory
        recipeToChange.RenderSubTitle()

        If ContentLoaded() Then
            If _RecipeList.Contains(recipeToChange) Then
                changed = True
            Else
                Dim copy = GetRecipe(oldCategpry, recipeToChange.Name)
                If copy IsNot Nothing Then
                    copy.Category = newCategory
                    copy.RenderSubTitle()
                    changed = True
                End If
            End If

            If changed And ContentIsGrouped Then
                CreateCategoryGroups()
            End If
        End If
    End Sub

#End Region

End Class

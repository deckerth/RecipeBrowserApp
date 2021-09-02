Imports Windows.Storage
Imports Windows.Storage.Pickers
Imports RecipeBrowser.Persistency
Imports Windows.UI.Popups

Public Class MetaDataDatabase

#Region "Properties"
    Public ReadOnly Property IsInitialized As Boolean
        Get
            Return MetadataDB IsNot Nothing
        End Get
    End Property
#End Region

    Private Shared _current As MetaDataDatabase
    Public Shared ReadOnly Property Current As MetaDataDatabase
        Get
            If _current Is Nothing Then
                _current = New MetaDataDatabase()
            End If
            Return _current
        End Get
    End Property

    Private Dirty As Boolean

    Private MetadataDB As Persistency.MetaDataAccess

    Public Sub New()
        _current = Me
        AddHandler App.Current.Suspending, AddressOf OnAppSuspending
        AddHandler App.Current.Resuming, AddressOf OnAppResuming
        Init()
    End Sub

#Region "Lifecycle"
    Public Sub Init()
        If MetadataDB IsNot Nothing Then
            App.Logger.Write("MetaDataDatabase.Init: MetadataDB IsNot Nothing")
        Else
            MetadataDB = New MetaDataAccess
        End If

        If RecipeFolders.Current IsNot Nothing AndAlso RecipeFolders.Current.GetRootFolder() IsNot Nothing Then
            MetadataDB.OpenDatabase()
            MetadataDB = MetaDataAccess.Current ' A null-operation - but if something went wrong, this reference is nothing
        End If

    End Sub

    Public Async Function CreateBackupAndCloseAsync() As Task
        If Dirty AndAlso RecipeFolders.Current.GetRootFolder() IsNot Nothing Then
            Await MetadataDB.CreateBackupAndCloseAsync(RecipeFolders.Current.GetRootFolder())
            Dirty = False
        ElseIf MetadataDB IsNot Nothing Then
            MetadataDB.CloseDatabase()
            MetadataDB = Nothing
        End If
    End Function

    Public Async Function TryRestoreBackupAsync() As Task

        If RecipeFolders.Current IsNot Nothing AndAlso RecipeFolders.Current.GetRootFolder() IsNot Nothing Then
            Dim rootFolder = RecipeFolders.Current.GetRootFolder()

            MetadataDB = New Persistency.MetaDataAccess

            Await MetadataDB.TryRestoreBackupAsync(rootFolder)
            MetadataDB.OpenDatabase()

            MetadataDB = Persistency.MetaDataAccess.Current
        End If

    End Function

    Private Async Sub OnAppSuspending(sender As Object, e As Windows.ApplicationModel.SuspendingEventArgs)

        'Get a deferral before performing any async operations
        'to avoid suspension
        Dim Deferral = e.SuspendingOperation.GetDeferral()

        App.Logger.Write("Closing metadata database")
        Await CreateBackupAndCloseAsync()

        Deferral.Complete()
    End Sub

    Private Sub OnAppResuming(sender As Object, e As Object)
        Init()
        App.Logger.Write("Metadata DB initialized")
    End Sub

#End Region

#Region "Read Access"

    Public Function GetCalories(category As String, title As String) As Integer

        If MetadataDB Is Nothing Then
            Return Recipe.CaloriesNotScanned
        Else
            Return MetadataDB.GetMetadata(category, title).Calories
        End If

    End Function

    Public Function GetMetadataForCategory(Category As String) As RecipeMetaDataList
        If MetadataDB IsNot Nothing Then
            Return MetadataDB.GetMetadataForCategory(Category)
        Else
            Return New Persistency.RecipeMetaDataList
        End If
    End Function

    Public Function GetTags(Category As String, Title As String) As List(Of RecipeTag)
        If MetadataDB IsNot Nothing AndAlso Category IsNot Nothing AndAlso Title IsNot Nothing Then
            Return MetadataDB.GetTags(Category, Title)
        Else
            Return New List(Of RecipeTag)
        End If
    End Function

    Public Function GetRecipesForTag(Tag As String) As List(Of RecipeTag)
        If MetadataDB IsNot Nothing Then
            Return MetadataDB.GetRecipesForTag(Tag)
        Else
            Return New List(Of RecipeTag)
        End If
    End Function

    Public Function GetTagDirectory() As List(Of TagDirectory)
        If MetadataDB IsNot Nothing Then
            Return MetadataDB.GetTagDirectory()
        Else
            Return New List(Of TagDirectory)
        End If
    End Function

#End Region

#Region "Change Metadata"

    Public Sub StoreCalories(ByRef aCategory As String, ByRef aTitle As String, kcal As Integer)
        If MetadataDB Is Nothing Then
            Return
        End If
        MetadataDB.StoreCalories(aCategory, aTitle, kcal)
        Dirty = True
    End Sub

    Public Sub StoreTags(ByRef aCategory As String, ByRef aTitle As String, tags As List(Of String))
        If MetadataDB Is Nothing Then
            Return
        End If
        MetadataDB.StoreTags(aCategory, aTitle, tags)
        Dirty = True
    End Sub

    Public Sub AddTag(newTag As TagDirectory)
        If MetadataDB Is Nothing Then
            Return
        End If
        MetadataDB.AddTag(newTag)
        Dirty = True
    End Sub

    Public Sub DeleteTag(toDelete As String)
        If MetadataDB Is Nothing Then
            Return
        End If
        MetadataDB.DeleteTag(toDelete)
        Dirty = True
    End Sub

    Public Sub RenameTag(oldTag As String, newTag As TagDirectory)
        If MetadataDB Is Nothing Then
            Return
        End If
        MetadataDB.RenameTag(oldTag, newTag)
        Dirty = True
    End Sub

    Public Sub RenameCategory(ByRef OldName As String, ByRef NewName As String)

        If MetadataDB Is Nothing Then
            Return
        End If

        MetadataDB.RenameCategory(OldName, NewName)
        Dirty = True
    End Sub

    Public Sub ChangeCategory(ByRef recipeToChange As Recipe, ByRef oldCategpry As String, ByRef newCategory As String)
        If MetadataDB Is Nothing Then
            Return
        End If

        MetadataDB.ChangeCategory(recipeToChange.Name, oldCategpry, newCategory)
        Dirty = True

    End Sub

    Public Sub RenameRecipe(ByRef recipeToRename As Recipe, ByVal oldName As String, ByVal newName As String)

        If MetadataDB Is Nothing Then
            Return
        End If

        MetadataDB.RenameRecipe(oldName, recipeToRename.Category, newName)
        Dirty = True

    End Sub

#End Region

#Region "ExportImport"

    Public Async Function ExportMetadataAsync() As Task

        Dim dialog As MessageDialog
        If MetadataDB IsNot Nothing Then

            Dim picker = New FileSavePicker
            Dim metadataFile As StorageFile
            Dim extensions As New List(Of String)
            Dim fileName As String

            extensions.Add(".db")
            fileName = "Metadata_" + Date.Today.Date.ToString.Substring(0, 10) + ".db"

            picker.DefaultFileExtension = ".db"
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            picker.FileTypeChoices.Clear()
            picker.FileTypeChoices.Add("Metadata", extensions)
            picker.SuggestedFileName = fileName

            Try
                metadataFile = Await picker.PickSaveFileAsync()
            Catch ex As Exception
                Return
            End Try

            If metadataFile Is Nothing Then
                Return
            End If

            If Await MetadataDB.ExportMetadata(metadataFile) Then
                dialog = New MessageDialog(App.Texts.GetString("ExportSuccess"))
                Await dialog.ShowAsync()
                Return
            End If
        End If

        dialog = New MessageDialog(App.Texts.GetString("ExportFailed"))
        Await dialog.ShowAsync()
    End Function

    Private Cancelled As Boolean
    Private ImportMode As DataImportDialogContent.ImportMode

    Public Async Function ImportMetadataAsync() As Task

        If MetadataDB Is Nothing Then
            MetadataDB = New MetaDataAccess
        End If

        Dim openPicker = New FileOpenPicker With {
            .SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            .ViewMode = PickerViewMode.Thumbnail
        }

        ' Filter to include a sample subset of file types.
        openPicker.FileTypeFilter.Clear()
        openPicker.FileTypeFilter.Add(".db")

        ' Open the file picker.
        Dim metadataFile As StorageFile = Await openPicker.PickSingleFileAsync()

        ' file is null if user cancels the file picker.
        Dim displayFailureMessage As Boolean = False
        If metadataFile IsNot Nothing Then
            Try
                Dim temp As StorageFile

                temp = Await ApplicationData.Current.LocalFolder.CreateFileAsync("metadata_to_import.db", CreationCollisionOption.ReplaceExisting)
                Await metadataFile.CopyAndReplaceAsync(temp)
                Await AskForImportMode(temp)
                If Not Cancelled Then
                    If ImportMode = DataImportDialogContent.ImportMode.Add Then
                        Dim caloricInfoUpdates As Integer
                        Dim recipeTagsUpdates As Integer
                        Dim newTagDirectoryEntries As Integer
                        MetadataDB.MergeMetadata(temp, caloricInfoUpdates, recipeTagsUpdates, newTagDirectoryEntries)
                        Dim resultDialog = New DataImportResultDialog With {.CaloricInfoUpdates = caloricInfoUpdates, .RecipeTagsUpdates = recipeTagsUpdates, .NewTagDirectoryEntries = newTagDirectoryEntries}
                        Await resultDialog.ShowAsync()
                    Else
                        If Await MetadataDB.ImportMetadata(metadataFile) Then
                            Dim dialog = New MessageDialog(App.Texts.GetString("ImportSuccess"))
                            Await dialog.ShowAsync()
                        Else
                            displayFailureMessage = True
                        End If
                    End If
                End If
            Catch ex As Exception
                displayFailureMessage = True
            End Try

            If displayFailureMessage Then
                Dim dialog = New MessageDialog(App.Texts.GetString("ImportFailed"))
                Await dialog.ShowAsync()
            End If
        End If

    End Function

    Private Async Function AskForImportMode(metadataFile As StorageFile) As Task
        Dim content = New DataImportDialogContent With {.DialogMode = DataImportDialogContent.ContentMode.Metadata}
        If Not MetadataDB.GetStatistics(content.CurrentDbCaloricInfos, content.CurrentDbRecipeTags, content.CurrentDbTagDirectoryEntries) Then
            Return
        End If

        If Not MetadataDB.GetStatistics(metadataFile, content.ImportDbCaloricInfos, content.ImportDbRecipeTags, content.ImportDbTagDirectoryEntries) Then
            Return
        End If

        Dim dialog = New ContentDialog With {
            .Title = App.Texts.GetString("ImportDatabaseTitle"),
            .PrimaryButtonText = App.Texts.GetString("AddEntries"),
            .SecondaryButtonText = App.Texts.GetString("ReplaceEntries"),
            .CloseButtonText = App.Texts.GetString("Cancel"),
            .DefaultButton = ContentDialogButton.Primary,
            .Content = content,
            .CloseButtonCommand = content.CloseButtonCommand,
            .PrimaryButtonCommand = content.PrimaryButtonCommand,
            .SecondaryButtonCommand = content.SecondaryButtonCommand
        }
        Await dialog.ShowAsync()
        ImportMode = content.SelectedMode
        Cancelled = content.Cancelled
        Return
    End Function


#End Region

End Class

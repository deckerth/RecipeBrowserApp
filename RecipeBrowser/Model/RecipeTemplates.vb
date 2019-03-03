Imports Windows.Storage

Public Class RecipeTemplates
    Inherits RecipeFolder

    Public Shared FolderName As String = App.Texts.GetString("TemplatesFolder")

    Public Sub New(folder As StorageFolder)
        MyBase.New(folder)

        _refreshVisibility = Visibility.Visible
        _scanForCaloriesVisibility = Visibility.Collapsed
        _editTagsVisibility = Visibility.Collapsed
        _recipeSearchBoxVisibility = Visibility.Collapsed
        _changeCategoryVisibility = Visibility.Collapsed
        _deleteRecipeVisibility = Visibility.Collapsed
        _renameRecipeVisibility = Visibility.Collapsed
        _editNoteVisibility = Visibility.Collapsed
        _editFileVisibility = Visibility.Collapsed
        _logAsCookedVisibility = Visibility.Collapsed
        _shareVisibility = Visibility.Visible
        _showImageGalleryVisibility = Visibility.Collapsed
        _addRecipeVisibility = Visibility.Visible
        _changeCalories = Visibility.Collapsed
        _openFileVisibility = Visibility.Collapsed
        _exportCaloricInfosVisibility = Visibility.Collapsed
    End Sub

    Public Shared Async Function CreateTemplateFolder(imageFolder As StorageFolder) As Task(Of StorageFolder)

        Dim rootfolder As StorageFolder = RecipeFolders.Current.GetRootFolder()
        Dim templatefolder As StorageFolder

        If rootfolder Is Nothing Then
            Return Nothing
        End If
        Try
            templatefolder = Await rootfolder.CreateFolderAsync(FolderName)
            If templatefolder Is Nothing Then
                Return Nothing
            End If
        Catch ex As Exception
            App.Logger.Write("Unable to create template folder" + ex.ToString)
            Return Nothing
        End Try

        Dim install As StorageFolder = Package.Current.InstalledLocation()
        Dim assets As StorageFolder
        Try
            assets = Await install.GetFolderAsync("Assets")
            If assets Is Nothing Then
                Return templatefolder
            End If
        Catch ex As Exception
            App.Logger.Write("Unable to access assets: " + ex.ToString)
            Return templatefolder
        End Try

        If imageFolder IsNot Nothing Then
            Await AssetAccess.CopyAssetFile(assets, imageFolder, "TemplateIcon.png", FolderName + ".png")
        End If

        Dim assetFiles = Await assets.GetFilesAsync()

        For Each file In assetFiles
            If file.Name.StartsWith(App.Texts.GetString("template1")) Then
                Await AssetAccess.CopyAssetFile(assets, templatefolder, file.Name)
                Continue For
            End If
            If file.Name.StartsWith(App.Texts.GetString("template2")) Then
                Await AssetAccess.CopyAssetFile(assets, templatefolder, file.Name)
                Continue For
            End If
        Next

        Return templatefolder
    End Function

    Public Shared Function GetSourceFilename(pdfFileName As String) As String

        Dim plainName As String = pdfFileName.Remove(4)
        Return Nothing

    End Function

End Class

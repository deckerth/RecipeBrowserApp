Imports Windows.Storage

Public Class HelpDocuments
    Inherits RecipeFolder

    Public Shared FolderName As String = App.Texts.GetString("HelpFolder")

    Public Sub New(folder As StorageFolder)
        MyBase.New(folder)

        _refreshVisibility = Visibility.Collapsed
        _scanForCaloriesVisibility = Visibility.Collapsed
        _editTagsVisibility = Visibility.Collapsed
        _changeCategoryVisibility = Visibility.Collapsed
        _deleteRecipeVisibility = Visibility.Collapsed
        _renameRecipeVisibility = Visibility.Collapsed
        _editFileVisibility = Visibility.Collapsed
        _logAsCookedVisibility = Visibility.Collapsed
        _shareVisibility = Visibility.Collapsed
        _showImageGalleryVisibility = Visibility.Collapsed
        _addRecipeVisibility = Visibility.Collapsed
        _changeCalories = Visibility.Collapsed
        _changeSortOrderVisibility = Visibility.Collapsed
        _exportCaloricInfosVisibility = Visibility.Collapsed
        _helpVisibility = Visibility.Visible

        _searchBoxSpaceHolderWidth = 20
    End Sub

    Private Shared Async Function CopyHelpFiles(helpFolder As StorageFolder) As Task

        Dim install As StorageFolder = Package.Current.InstalledLocation()
        Dim assets As StorageFolder
        Try
            assets = Await install.GetFolderAsync("Assets")
            If assets Is Nothing Then
                Return
            End If
        Catch ex As Exception
            App.Logger.Write("Unable to access assets: " + ex.ToString)
            Return
        End Try

        Dim assetFiles = Await assets.GetFilesAsync()

        For Each file In assetFiles
            If file.Name.StartsWith(App.Texts.GetString("helpPattern")) Then
                Await AssetAccess.CopyAssetFile(assets, helpFolder, file.Name, file.Name.Substring(7))
                Continue For
            End If
        Next

    End Function


    Public Shared Async Function CreateHelpFolder() As Task(Of StorageFolder)

        Dim rootfolder As StorageFolder = RecipeFolders.Current.GetRootFolder()
        Dim helpFolder As StorageFolder

        If rootfolder Is Nothing Then
            Return Nothing
        End If
        Try
            helpFolder = Await rootfolder.CreateFolderAsync(FolderName)
            If helpFolder Is Nothing Then
                Return Nothing
            End If
        Catch ex As Exception
            App.Logger.Write("Unable to create help folder" + ex.ToString)
            Return Nothing
        End Try

        Await CopyHelpFiles(helpFolder)

        Return helpFolder
    End Function

    Shared Async Function UpdateHelpFolder(helpFolder As StorageFolder) As Task

        Dim localSettings = Windows.Storage.ApplicationData.Current.LocalSettings

        Dim installedVersion As String
        Try
            installedVersion = localSettings.Values("InstalledVersion")
        Catch ex As Exception
        End Try

        If installedVersion Is Nothing OrElse installedVersion < App.AppVersion Then
            Await CopyHelpFiles(helpFolder)
            localSettings.Values("InstalledVersion") = App.AppVersion
        End If

    End Function

End Class

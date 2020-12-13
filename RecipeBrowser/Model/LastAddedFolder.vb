Imports Windows.Globalization.DateTimeFormatting
Imports Windows.Storage.Search

Public Class LastAddedFolder
    Inherits RecipeFolder

    Public Shared FolderName As String = "LastAddedFolder"
    Public Shared Current As LastAddedFolder

    Public Property CurrentAddedSince As DateTime = SearchResults.UndefinedDate
    Public Property LastAddedSince As DateTime = SearchResults.UndefinedDate

    Public Sub New()
        MyBase.New(FolderName)
        Current = Me

        ContentIsGrouped = True

        _addRecipeVisibility = Visibility.Collapsed
    End Sub

    Public Overrides Function IsSpecialFolder() As Boolean
        Return True
    End Function

    Public Sub SetSearchParameter(startDate As DateTime)
        Try
            Dim dto As DateTimeOffset = New DateTimeOffset(startDate)
            DisplayName = App.Texts.GetString("AddedSince") + " " + DateTimeFormatter.ShortDate.Format(dto)
            LastAddedSince = CurrentAddedSince
            CurrentAddedSince = startDate
            _ContentLoaded = False
        Catch ex As Exception
        End Try
    End Sub

    Public Overrides Async Function LoadAsync() As Task

        If _ContentLoaded OrElse CurrentAddedSince = SearchResults.UndefinedDate Then
            Return
        End If

        LastAddedSince = CurrentAddedSince

        _RecipeList.Clear()
        _Recipes.Clear()

        Dim FolderDirectory = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        Dim addedSince As String = CurrentAddedSince.ToString

        Dim queryOptions = New Windows.Storage.Search.QueryOptions()
        queryOptions.FolderDepth = Windows.Storage.Search.FolderDepth.Deep
        queryOptions.ApplicationSearchFilter = "System.DateCreated:>=" + addedSince

        Dim startFolder = Await FolderDirectory.GetStorageFolderAsync("")
        Dim query = startFolder.CreateFileQueryWithOptions(queryOptions)

        Dim result = Await query.GetFilesAsync()

        ' If the search is done on root-folder level, results in help are skipped
        Await SetUpFolderFromFileListAsync(result, True, "", addedDateFilter:=CurrentAddedSince)

    End Function

    Sub Clear()
    End Sub

End Class

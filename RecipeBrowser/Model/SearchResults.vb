﻿Imports Windows.Storage

Public Class SearchResults
    Inherits RecipeFolder

    Public Shared FolderName As String = App.Texts.GetString("SearchResultFolder")
    Public Property LastSearchString As String

    Public Sub New()
        MyBase.New(FolderName)
        ContentIsGrouped = True

        _refreshVisibility = Visibility.Collapsed
        _recipsSearchBoxEnabled = False
        _addRecipeVisibility = Visibility.Collapsed
    End Sub

    Public Overrides Function IsSpecialFolder() As Boolean
        Return True
    End Function

    Public Sub SetSearchParameter(SearchFolder As String, ByVal SearchString As String, Optional SearchTag As String = "")

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        roamingSettings.Values("SearchString") = New String(SearchString)
        roamingSettings.Values("SearchFolder") = New String(SearchFolder)
        roamingSettings.Values("SearchTag") = New String(SearchTag)
        _ContentLoaded = False
        LastSearchString = SearchString

    End Sub

    Public Overrides Async Function LoadAsync() As Task

        If _ContentLoaded Then
            Return
        End If

        _RecipeList.Clear()
        _Recipes.Clear()

        Dim FolderDirectory = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        Dim searchString = roamingSettings.Values("SearchString")
        Dim searchFolder = roamingSettings.Values("SearchFolder")
        Dim searchTag = roamingSettings.Values("SearchTag")

        If searchFolder Is Nothing Or searchString Is Nothing Then
            Return
        End If

        Dim queryOptions = New Windows.Storage.Search.QueryOptions()
        queryOptions.ApplicationSearchFilter = searchString
        queryOptions.IndexerOption = Windows.Storage.Search.IndexerOption.UseIndexerWhenAvailable
        queryOptions.FolderDepth = Windows.Storage.Search.FolderDepth.Deep

        Dim startFolder = Await FolderDirectory.GetStorageFolderAsync(searchFolder)
        Dim query = startFolder.CreateFileQueryWithOptions(queryOptions)

        Dim result = Await query.GetFilesAsync()

        ' If the search is done on root-folder level, results in help are skipped
        Await SetUpFolderFromFileListAsync(result,
                                           skipResultsInHelp:=startFolder.Equals(RecipeFolders.Current.GetRootFolder()),
                                           tagFilter:=searchTag)

    End Function

    Sub Clear()
        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        roamingSettings.Values("SearchString") = ""
        roamingSettings.Values("SearchFolder") = ""
        roamingSettings.Values("SearchTag") = ""
    End Sub

    'Public Sub ChangeCategory(ByRef recipeToChange As Recipe, ByRef NewCategory As String)

    '    If Not ContentLoaded() Then
    '        Return
    '    End If

    '    Dim instanceInSearchResults As Recipe = GetRecipe(recipeToChange.Category, recipeToChange.Name)
    '    If instanceInSearchResults Is Nothing Then
    '        Return
    '    End If

    '    If Not ReferenceEquals(instanceInSearchResults, recipeToChange) Then
    '        instanceInSearchResults.Category = NewCategory
    '        instanceInSearchResults.RenderSubTitle()
    '        instanceInSearchResults.Notes = recipeToChange.Notes
    '    End If

    'End Sub

End Class

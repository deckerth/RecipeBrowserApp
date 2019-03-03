Imports RecipeBrowser.Persistency

Public Class TagFolder
    Inherits RecipeFolder

    Public Property Tag As TagDirectory

    Public Sub New(aTag As String)
        MyBase.New(aTag)
        Tag = TagRepository.Current.GetTag(aTag)
        If Tag Is Nothing Then
            Tag = New TagDirectory(aTag)
        End If
        Background = Tag.GetBackground()
        Foreground = Tag.GetForeground()
        ContentIsGrouped = True

        _refreshVisibility = Visibility.Collapsed
        _addRecipeVisibility = Visibility.Collapsed
    End Sub

    Public Overrides Function IsLabelFolder() As Boolean
        Return True
    End Function

    Public Overrides Async Function LoadAsync() As Task
        Dim recipes = MetaDataDatabase.Current.GetRecipesForTag(Tag.Tag)
        Dim allFolders = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        _RecipeList.Clear()

        Try
            LoadProgressTotal = recipes.Count
            LoadProgressValue = 0
            For Each item In recipes
                LoadProgressValue = LoadProgressValue + 1
                If RecipesPage.Current IsNot Nothing AndAlso
                    RecipesPage.Current.CurrentRecipeFolder.Equals(Me) Then
                    RecipesPage.Current.SetLoadProgress(LoadProgressValue)
                End If

                Dim newRecipe As Recipe

                newRecipe = Await allFolders.GetRecipeAsync(item.Category, item.Category, item.Title)

                If newRecipe IsNot Nothing Then
                    newRecipe.AddedToFavoritesDateTime = newRecipe.CreationDateTime
                    newRecipe.RenderSubTitle()
                    _RecipeList.Add(newRecipe)
                End If
            Next

            ApplySortOrder()

            _ContentLoaded = True
        Catch ex As Exception
            App.Logger.WriteException("TagFolder.LoadAsync", ex)
        End Try
    End Function

#Region "Delete folder"
    Public Overrides Async Function DeleteFolder() As Task
        Await Task.Run(Sub()
                           TagRepository.Current.DeleteTag(Name)
                       End Sub)
    End Function
#End Region


End Class

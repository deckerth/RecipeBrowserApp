﻿Imports Windows.Storage

Public Class Favorites
    Inherits RecipeFolder

    Public Shared FolderName As String = App.Texts.GetString("FavoritesFolder")

    Class RecipeDescriptor
        Public Category As String
        Public Name As String
        Public IsExternal As Boolean
        Public AddedToFavorites As DateTime
    End Class

    Private FavoritesList As List(Of RecipeDescriptor)

    Public Sub New()
        MyBase.New(FolderName)

        _favoritesVisibility = Visibility.Collapsed
        _recipeSearchBoxVisibility = Visibility.Collapsed
        _addRecipeVisibility = Visibility.Collapsed
    End Sub

    Public Overrides Function IsSpecialFolder() As Boolean
        Return True
    End Function

    Private Sub LoadFavorites()

        If FavoritesList IsNot Nothing Then
            Return
        End If

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        Dim recipeList = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)

        FavoritesList = New List(Of RecipeDescriptor)

        For Each item In recipeList.Values
            Dim recipeComposite As ApplicationDataCompositeValue = item.Value
            Dim folder As String
            Dim recipe As String
            Dim isExternal As Boolean
            Dim added As DateTime
            Try
                folder = recipeComposite("Folder")
                recipe = recipeComposite("Recipe")
                isExternal = recipeComposite("IsExternal")
                If Not DateTime.TryParse(recipeComposite("AddedToFavorites"), added) Then
                    added = DateTime.MinValue
                End If
            Catch ex As Exception
            End Try
            If folder IsNot Nothing AndAlso recipe IsNot Nothing Then
                If folder.Equals(SearchResults.FolderName) Then
                    ' Delete this entry
                    recipeList.Values.Remove(item.Key)
                Else
                    Dim newRecipe As New RecipeDescriptor
                    newRecipe.Category = folder
                    newRecipe.Name = recipe
                    newRecipe.IsExternal = isExternal
                    newRecipe.AddedToFavorites = added
                    FavoritesList.Add(newRecipe)
                End If
            End If
        Next

    End Sub

    Public Async Function AddRecipeAsync(ByVal newRecipe As Recipe) As Task
        If Not _ContentLoaded Then
            Await LoadAsync()
        End If
        If GetRecipe(newRecipe.Category, newRecipe.Name) Is Nothing Then
            newRecipe.AddedToFavoritesDateTime = DateTime.Now
            newRecipe.RenderSubTitle()
            _RecipeList.Add(newRecipe)
            ApplySortOrder()

            Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
            Dim recipeList = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)
            Dim recipeComposite = New Windows.Storage.ApplicationDataCompositeValue()
            recipeComposite("Folder") = newRecipe.Category
            recipeComposite("Recipe") = newRecipe.Name
            recipeComposite("IsExternal") = (newRecipe.ItemType = Recipe.ItemTypes.ExternalRecipe)
            recipeComposite("AddedToFavorites") = newRecipe.AddedToFavoritesDateTime.ToString
            recipeList.Values(Guid.NewGuid().ToString()) = recipeComposite
        End If
    End Function

    Public Overrides Function DeleteRecipe(ByRef recipeToDelete As Recipe) As Boolean

        If Not MyBase.DeleteRecipe(recipeToDelete) Then
            Return False
        End If

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        Dim recipeList As ApplicationDataContainer = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)
        Dim index As String
        For Each item In recipeList.Values
            Dim recipeComposite As ApplicationDataCompositeValue = item.Value
            If recipeComposite("Recipe").Equals(recipeToDelete.Name) AndAlso
               recipeComposite("Folder").Equals(recipeToDelete.Category) Then
                index = item.Key
                Exit For
            End If
        Next
        If index IsNot Nothing Then
            recipeList.Values.Remove(index)
        End If

        Return True
    End Function

    Public Overrides Async Function LoadAsync() As Task

        Dim allFolders = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        If FavoritesList Is Nothing Then
            LoadFavorites()
        End If

        _RecipeList.Clear()

        LoadProgressTotal = FavoritesList.Count
        LoadProgressValue = 0
        For Each item In FavoritesList
            LoadProgressValue = LoadProgressValue + 1
            If RecipesPage.Current IsNot Nothing AndAlso
                    RecipesPage.Current.CurrentRecipeFolder.Equals(Me) Then
                RecipesPage.Current.SetLoadProgress(LoadProgressValue)
            End If

            Dim newRecipe As Recipe

            If item.IsExternal Then
                newRecipe = New ExternalRecipe(item.Category, item.Name)
            Else
                newRecipe = Await allFolders.GetRecipeAsync(item.Category, item.Category, item.Name)
            End If
            If newRecipe IsNot Nothing Then
                If item.AddedToFavorites = DateTime.MinValue Then
                    newRecipe.AddedToFavoritesDateTime = newRecipe.CreationDateTime
                Else
                    newRecipe.AddedToFavoritesDateTime = item.AddedToFavorites
                End If
                newRecipe.RenderSubTitle()
                _RecipeList.Add(newRecipe)
            End If
        Next

        ApplySortOrder()

        _ContentLoaded = True
    End Function

    Public Function IsFavorite(recipeToLookup As Recipe) As Boolean

        If ContentLoaded() Then
            Return GetRecipe(recipeToLookup.Category, recipeToLookup.Name) IsNot Nothing
        Else
            If FavoritesList Is Nothing Then
                LoadFavorites()
            End If

            Dim matches = FavoritesList.Where(Function(otherRecipe) otherRecipe.Name.Equals(recipeToLookup.Name) And otherRecipe.Category.Equals(recipeToLookup.Category))
            Return matches.Count() = 1
        End If

    End Function

    Public Sub RenameCategory(ByRef OldName As String, ByRef NewName As String)

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        Dim storedRecords As ApplicationDataContainer = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)
        Dim toRename As New List(Of String)

        For Each item In storedRecords.Values
            Dim recipeComposite As ApplicationDataCompositeValue = item.Value
            Try
                Dim f As String = recipeComposite("Folder")
                If f IsNot Nothing AndAlso f.Equals(OldName) Then
                    toRename.Add(item.Key)
                End If
            Catch ex As Exception
            End Try
        Next

        For Each key In toRename
            Dim recipeComposite = New Windows.Storage.ApplicationDataCompositeValue()
            recipeComposite("Folder") = NewName
            recipeComposite("Recipe") = storedRecords.Values(key)("Recipe")
            recipeComposite("IsExternal") = storedRecords.Values(key)("IsExternal")
            storedRecords.Values.Remove(key)
            storedRecords.Values(key) = recipeComposite
        Next

        Invalidate()
        FavoritesList = Nothing

    End Sub

    Public Overrides Sub ChangeCategory(ByRef recipeToChange As Recipe, ByRef oldCategpry As String, ByRef newCategory As String)

        MyBase.ChangeCategory(recipeToChange, oldCategpry, newCategory)

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        Dim storedRecords = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)

        For Each item In storedRecords.Values
            Dim recipeComposite As ApplicationDataCompositeValue = item.Value
            If recipeComposite("Folder") = recipeToChange.Category And recipeComposite("Recipe") = recipeToChange.Name Then
                recipeComposite("Folder") = newCategory
                recipeComposite("Recipe") = recipeToChange.Name
                storedRecords.Values.Remove(item.Key)
                storedRecords.Values(item.Key) = recipeComposite
                Return
            End If
        Next

    End Sub

    Protected Overrides Sub ApplySortOrder()
        Dim _comparer As IComparer(Of Recipe)
        Select Case _SortOrder
            Case SortOrder.ByNameAscending
                _comparer = New RecipeComparer_NameAscending
            Case SortOrder.ByDateDescending
                _comparer = New RecipeComparer_AddedToFavoritesDescending
            Case SortOrder.ByLastCookedDescending
                _comparer = New RecipeComparer_LastCookedDescending
        End Select
        If _comparer IsNot Nothing Then
            _RecipeList.Sort(_comparer)
        End If
        _Recipes.Clear()

        For Each item In _RecipeList
            _Recipes.Add(item)
        Next
    End Sub

#Region "RenameRecipe"

    Public Overrides Sub RenameRecipe(ByRef recipeToRename As Recipe, ByVal oldName As String, ByVal newName As String)


        MyBase.RenameRecipe(recipeToRename, oldName, newName)

        Dim roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings
        Dim storedRecords As ApplicationDataContainer = roamingSettings.CreateContainer("Favorites", Windows.Storage.ApplicationDataCreateDisposition.Always)

        For Each item In storedRecords.Values
            Dim recipeComposite As ApplicationDataCompositeValue = item.Value
            Dim currentCategory As String
            Dim currentRecipe As String
            Dim isExternal As Boolean
            Try
                currentCategory = recipeComposite("Folder")
                currentRecipe = recipeComposite("Recipe")
                isExternal = recipeComposite("IsExternal")
            Catch ex As Exception
                Continue For
            End Try

            If currentCategory = recipeToRename.Category And currentRecipe = oldName Then
                Dim newRecipeComposite As Windows.Storage.ApplicationDataCompositeValue = New Windows.Storage.ApplicationDataCompositeValue()
                newRecipeComposite("Folder") = recipeToRename.Category
                newRecipeComposite("Recipe") = newName
                newRecipeComposite("IsExternal") = isExternal
                storedRecords.Values.Remove(item.Key)
                storedRecords.Values(item.Key) = newRecipeComposite
                Return
            End If
        Next
    End Sub

#End Region

End Class

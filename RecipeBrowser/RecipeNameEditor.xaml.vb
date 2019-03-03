' Die Elementvorlage "Inhaltsdialog" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Imports Windows.Storage

Public NotInheritable Class RecipeNameEditor
    Inherits ContentDialog

    Class StringContainer
        Public content As New String("")
    End Class

    Public Enum FileActions
        Copy
        Rename
        NewRecipe
        NewFolder
        RenameFolder
    End Enum

    Private currentCategory As RecipeFolder
    Private newRecipe As StorageFile
    Private oldName As String
    Private cancelled As Boolean
    Private intendedAction As FileActions

    Public Sub SetCategory(ByRef TheCurrentCategory As RecipeFolder)
        currentCategory = TheCurrentCategory
    End Sub

    Public Sub SetTitle(ByRef TheTitle As String)
        RecipeName.Text = TheTitle
        oldName = RecipeName.Text
        CheckInput()
    End Sub

    Public Sub SetFile(ByRef TheNewFile As StorageFile)
        newRecipe = TheNewFile
    End Sub

    Public Sub SetAction(ByRef action As FileActions)
        intendedAction = action

        Select Case intendedAction
            Case FileActions.Rename
                RecipeNameEditor.Title = App.Texts.GetString("RenameRecipeText")
                RecipeNameEditor.PrimaryButtonText = App.Texts.GetString("Rename")
            Case FileActions.NewFolder
                RecipeNameEditor.Title = App.Texts.GetString("NewSubCategoryText")
                RecipeNameLabel.Text = App.Texts.GetString("SubCategoryName")
            Case FileActions.RenameFolder
                RecipeNameEditor.Title = App.Texts.GetString("Rename")
                RecipeNameLabel.Text = App.Texts.GetString("SubCategoryName")
                RecipeNameEditor.PrimaryButtonText = App.Texts.GetString("Rename")
            Case FileActions.NewRecipe
                TemplateChooser.Visibility = Visibility.Visible
                TemplateChooserLabel.Visibility = Visibility.Visible
                TemplateChooser.ItemsSource = RecipeFolders.Current.GetFolder(RecipeTemplates.FolderName).Recipes
                RecipeNameEditor.IsPrimaryButtonEnabled = False
        End Select
    End Sub

    Public Function DialogCancelled() As Boolean
        Return cancelled
    End Function

    Public Function GetRecipeTitle() As String
        Return RecipeName.Text.Trim()
    End Function

    Public Function GetRecipeTemplate() As Recipe
        Return TemplateChooser.SelectedItem
    End Function

    Private Function RecipeNameIsValid(Optional ByRef errorMessage As StringContainer = Nothing) As Boolean

        If RecipeName.Text Is Nothing OrElse RecipeName.Text.Trim().Equals("") Then
            If errorMessage IsNot Nothing Then
                If intendedAction = FileActions.NewFolder OrElse intendedAction = FileActions.RenameFolder Then
                    errorMessage.content = App.Texts.GetString("FolderNameIsEmpty")
                Else
                    errorMessage.content = App.Texts.GetString("RecipeNameIsEmpty")
                End If
            End If
            Return False
        End If

        Dim title As String = RecipeName.Text.Trim()

        If intendedAction = FileActions.Copy OrElse intendedAction = FileActions.NewRecipe OrElse
                intendedAction = FileActions.NewFolder OrElse Not oldName.Equals(title) Then
            ' The existence check is skipped when renaming, and if the name has not been changed
            If currentCategory.GetRecipe(currentCategory.Name, title) IsNot Nothing OrElse
               currentCategory.GetSubcategory(currentCategory.Name, title) IsNot Nothing Then
                If errorMessage IsNot Nothing Then
                    errorMessage.content = App.Texts.GetString("RecipeDoesAlreadyExist")
                End If
                Return False
            End If
        End If

        If errorMessage IsNot Nothing Then
            errorMessage.content = ""
        End If
        Return True

    End Function

    Private Async Sub RecipeNameEditor_PrimaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)

        RecipeNameEditor.Hide()

        If intendedAction = FileActions.Copy AndAlso newRecipe IsNot Nothing Then
            Dim extension As String = newRecipe.Name.Substring(newRecipe.Name.Length - 4, 4)  ' test.pdf l=8
            Dim newFilename As String = RecipeName.Text.Trim() + extension
            Await newRecipe.CopyAsync(currentCategory.Folder, newFilename, NameCollisionOption.FailIfExists)
        End If

        cancelled = False

    End Sub

    Private Sub RecipeNameEditor_SecondaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)

        RecipeNameEditor.Hide()
        cancelled = True

    End Sub

    Private Sub CheckInput()

        Dim errorMessage As New StringContainer()

        If RecipeNameIsValid(errorMessage) Then
            RecipeName.SetValue(BorderBrushProperty, New SolidColorBrush(Windows.UI.Colors.Black))
            RecipeNameEditor.IsPrimaryButtonEnabled = True
        Else
            RecipeName.SetValue(BorderBrushProperty, New SolidColorBrush(Windows.UI.Colors.Red))
            RecipeNameEditor.IsPrimaryButtonEnabled = False
        End If

        ErrorMessageDisplay.Text = errorMessage.content

    End Sub

    Private Sub RecipeName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles RecipeName.TextChanged

        CheckInput()

    End Sub

End Class

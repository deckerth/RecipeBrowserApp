' Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

Public NotInheritable Class EditCaloriesDialog
    Inherits ContentDialog

    Private RecipeToEdit As Recipe

    Public Sub New(recipeToChange As Recipe)

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()

        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.
        RecipeToEdit = recipeToChange
        If RecipeToEdit.Calories > 0 Then
            CaloriesTextBox.Text = RecipeToEdit.Calories.ToString()
        End If

    End Sub

    Private Function GetCalories() As Integer
        Dim kcal As Integer
        Dim input As String = CaloriesTextBox.Text.Trim()

        If input.Length = 0 Then
            Return 0
        Else
            If Not Integer.TryParse(input, kcal) Then
                Return -1
            End If
            If kcal < 0 Then
                Return -1
            End If
        End If
        Return kcal
    End Function

    Private Sub ContentDialog_PrimaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)
        Dim kcal As Integer = GetCalories()

        If kcal >= 0 AndAlso RecipeToEdit.Calories <> kcal Then
            RecipeToEdit.Calories = kcal
            RecipeToEdit.RenderSubTitle()
            MetaDataDatabase.Current.StoreCalories(RecipeToEdit.Category, RecipeToEdit.Name, kcal)
            Hide()
        End If

    End Sub

    Private Sub ContentDialog_SecondaryButtonClick(sender As ContentDialog, args As ContentDialogButtonClickEventArgs)
        CaloriesEditor.Hide()
    End Sub

    Private Sub CaloriesTextBox_TextChanged(sender As Object, e As TextChangedEventArgs) Handles CaloriesTextBox.TextChanged
        IsPrimaryButtonEnabled = GetCalories() >= 0
    End Sub
End Class

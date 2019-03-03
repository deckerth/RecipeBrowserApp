Imports SQLite

Namespace Global.RecipeBrowser.Persistency

    Public Class RecipeMetaData
        <PrimaryKey()> <AutoIncrement()>
        Public Property Id As Integer

        Public Property Category As String
        Public Property Title As String
        Public Property Calories As Integer

        Public Sub New()
            Title = String.Empty
            Category = String.Empty
            Calories = RecipeBrowser.Recipe.CaloriesUnknown
        End Sub

        Public Sub New(theCategory As String, theTitle As String, kcal As Integer)

            Category = theCategory
            Title = theTitle
            Calories = kcal
        End Sub
    End Class

End Namespace

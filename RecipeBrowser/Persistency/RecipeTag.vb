Imports SQLite
Imports Windows.UI

Namespace Global.RecipeBrowser.Persistency

    Public Class RecipeTag
        <PrimaryKey()> <AutoIncrement()>
        Public Property Id As Integer

        Public Property Category As String
        Public Property Title As String
        Public Property Tag As String

        Public Sub New()
            Title = String.Empty
            Category = String.Empty
            Tag = String.Empty
        End Sub

        Public Sub New(theCategory As String, theTitle As String, aTag As String)

            Category = theCategory
            Title = theTitle
            Tag = aTag
        End Sub
    End Class

End Namespace

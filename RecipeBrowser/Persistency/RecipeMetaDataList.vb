Namespace Global.RecipeBrowser.Persistency

    Public Class RecipeMetaDataList

        Public Property Entries As New List(Of RecipeMetadata)

        Public Function GetMetadata(Title As String) As RecipeMetadata

            Dim result = Entries.Where(Function(otherRecipe) otherRecipe.Title.Equals(Title)).FirstOrDefault()
            If result Is Nothing Then
                Return New RecipeMetadata("", Title, RecipeBrowser.Recipe.CaloriesNotScanned)
            Else
                Return result
            End If

        End Function

    End Class

End Namespace

Public Class SubCategory
    Inherits Recipe

    Public Sub New(parentCategory As RecipeFolder, subCategoryName As String)
        ItemType = ItemTypes.SubCategory
        Category = parentCategory.Name
        Parent = parentCategory
        Name = subCategoryName
        SubTitle = App.Texts.GetString("SubCategory")
        FolderIconVisibility = Visibility.Visible
    End Sub

End Class

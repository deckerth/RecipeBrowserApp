Namespace Global.RecipeBrowser.TemplateSelectors

    Public Class TileTemplateSelector
        Inherits DataTemplateSelector

        Public Property DefaultDataDemplate As DataTemplate
        Public Property PlusDataTemplate As DataTemplate

        Protected Overrides Function SelectTemplateCore(item As Object) As DataTemplate
            Dim e = DirectCast(item, RecipeFolder)

            If e.IsPlus Then
                Return PlusDataTemplate
            Else
                Return DefaultDataDemplate
            End If
        End Function

        Protected Overrides Function SelectTemplateCore(item As Object, container As DependencyObject) As DataTemplate
            Dim e = DirectCast(item, RecipeFolder)

            If e.IsPlus Then
                Return PlusDataTemplate
            Else
                Return DefaultDataDemplate
            End If
        End Function

    End Class

End Namespace



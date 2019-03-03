Imports RecipeBrowser.Common
Imports Windows.UI

Namespace Global.RecipeBrowser.ViewModels

    Public Class RecipeTagViewModel
        Implements INotifyPropertyChanged

        Public Event PropertyChanged(ByVal sender As Object, ByVal e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

        Protected Overridable Sub OnPropertyChanged(ByVal PropertyName As String)
            ' Raise the event, and make this procedure
            ' overridable, should someone want to inherit from
            ' this class and override this behavior:
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(PropertyName))
        End Sub

        Public Property TaggedRecipe As Recipe
        Public Property Tag As String
        Public Property Background As Color
        Public Property Foreground As Color

        Public Property IsPlus As Boolean

        Public Property AddDeleteCommand As RelayCommand

        Public Sub New(aRecipe As Recipe, Optional aTag As String = "", Optional asPlus As Boolean = False)
            TaggedRecipe = aRecipe
            Tag = aTag
            IsPlus = asPlus
            If Not IsPlus Then
                Dim tagInfo = TagRepository.Current.GetTag(aTag)
                If tagInfo IsNot Nothing Then
                    Background = tagInfo.GetBackground()
                    Foreground = tagInfo.GetForeground()
                End If
            End If
            AddDeleteCommand = New RelayCommand(AddressOf OnAddDeleteCommand)
        End Sub

        Public Async Function OnAddDeleteCommand() As Task
            If IsPlus Then
                Await TaggedRecipe.AddTag()
            Else
                TaggedRecipe.DeleteTag(Me)
            End If
        End Function

    End Class

End Namespace

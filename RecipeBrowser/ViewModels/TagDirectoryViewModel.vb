Imports RecipeBrowser.Persistency
Imports Windows.UI

Namespace Global.RecipeBrowser.ViewModels

    Public Class TagDirectoryViewModel

        Dim model As TagDirectory

        Public Sub New()
            model = New TagDirectory
        End Sub

        Public Sub New(aTag As TagDirectory)
            model = aTag
        End Sub

        Public Sub New(aTag As String, BackgroundColor As Color, ForegroundColor As Color)
            model = New TagDirectory(aTag, BackgroundColor, ForegroundColor)
        End Sub

        Public Property Tag As String
            Get
                Return model.Tag
            End Get
            Set(value As String)
                model.Tag = Tag
            End Set
        End Property

        Public Property Background As Color
            Get
                Return model.GetBackground()
            End Get
            Set(value As Color)
                model.SetBackground(value)
            End Set
        End Property

        Public Property Foreground As Color
            Get
                Return model.GetForeground()
            End Get
            Set(value As Color)
                model.SetForeground(value)
            End Set
        End Property
    End Class

End Namespace

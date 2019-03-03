Imports SQLite
Imports Windows.UI

Namespace Global.RecipeBrowser.Persistency

    Public Class TagDirectory

        <PrimaryKey()> <AutoIncrement()>
        Public Property Id As Integer

        Public Property Tag As String
        Public Property Background_A As Byte = Colors.White.A
        Public Property Background_R As Byte = Colors.White.R
        Public Property Background_G As Byte = Colors.White.G
        Public Property Background_B As Byte = Colors.White.B
        Public Property Foreground_A As Byte = Colors.White.A
        Public Property Foreground_R As Byte = Colors.White.R
        Public Property Foreground_G As Byte = Colors.White.G
        Public Property Foreground_B As Byte = Colors.White.B

        Public Sub New()
            Tag = String.Empty
        End Sub

        Public Sub New(aTag As String, Optional Background As Color = Nothing, Optional Foreground As Color = Nothing)
            Tag = aTag
            If Background <> Nothing Then
                SetBackground(Background)
            End If
            If Foreground <> Nothing Then
                SetForeground(Foreground)
            End If
        End Sub

        Public Function GetForeground() As Color
            Return Color.FromArgb(Foreground_A, Foreground_R, Foreground_G, Foreground_B)
        End Function

        Public Function GetBackground() As Color
            Return Color.FromArgb(Background_A, Background_R, Background_G, Background_B)
        End Function

        Public Sub SetForeground(Foreground As Color)
            Foreground_A = Foreground.A
            Foreground_R = Foreground.R
            Foreground_G = Foreground.G
            Foreground_B = Foreground.B
        End Sub

        Public Sub SetBackground(Background As Color)
            Background_A = Background.A
            Background_R = Background.R
            Background_G = Background.G
            Background_B = Background.B
        End Sub

    End Class

End Namespace

Imports Windows.UI

Namespace Global.RtfEditor

    Public Class FontColorDescriptor

        Property Argb As Color
        Property XamlString As String

        Public Sub New(ByRef theColor As Color)
            Argb = theColor
            XamlString = Argb.ToString
        End Sub

    End Class

End Namespace

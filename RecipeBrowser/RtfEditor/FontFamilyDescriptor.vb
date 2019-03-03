Imports Windows.UI
Imports Windows.UI.Text

Namespace Global.RtfEditor

    Public Class FontFamilyDescriptor

        Property Title As String
        Property FontFamilyName As String
        Property FontSize As Integer
        Property FontWeight As FontWeight
        Property ForegroundColorBrush As SolidColorBrush

        Public Sub New()
            ForegroundColorBrush = New SolidColorBrush(Colors.Black)
        End Sub

        Public Sub New(ByRef theTitle As String, theFontFamilyName As String, theFontSize As Integer, theFontWeight As FontWeight, theColor As SolidColorBrush)
            Title = theTitle
            FontFamilyName = theFontFamilyName
            FontSize = theFontSize
            FontWeight = theFontWeight
            ForegroundColorBrush = theColor
        End Sub

    End Class

End Namespace

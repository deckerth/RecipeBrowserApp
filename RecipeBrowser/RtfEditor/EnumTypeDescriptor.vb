Imports Windows.UI.Text

Namespace Global.RtfEditor

    Public Class EnumTypeDescriptor

        Property Type As MarkerType
        Property Style As MarkerStyle
        Property Example As String

        Public Sub New(ByRef theType As MarkerType, theStyle As MarkerStyle, theExample As String)
            Type = theType
            Style = theStyle
            Example = theExample
        End Sub

    End Class

End Namespace

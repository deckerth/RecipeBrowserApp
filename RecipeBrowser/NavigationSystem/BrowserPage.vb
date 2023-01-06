Namespace Global.RecipeBrowser.NavigationSystem

    Public Class BrowserPageBase

    End Class

    Public Class BrowserPage
        Inherits BrowserPageBase

        Public Property Name As String
        Public Property Glyph As String
        Public Property IsFontAwesomeIcon As Boolean
        Public Property IsFontIcon As Boolean
        Public Property FontFamily As FontFamily
        Public Property FontSize As Double
        Public Property Tooltip As String
        Public Property TargetType As Type

    End Class

    Public Class Separator
        Inherits BrowserPageBase

    End Class

    Public Class Header
        Inherits BrowserPageBase

        Public Property Name As String
    End Class

End Namespace

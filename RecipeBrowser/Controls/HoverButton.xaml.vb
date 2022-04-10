' Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

Imports RecipeBrowser.Common

Public NotInheritable Class HoverButton
    Inherits UserControl

    Public Shared ReadOnly GlyphProperty As DependencyProperty = DependencyProperty.Register("Glyph",
                           GetType(String), GetType(HoverButton), New PropertyMetadata(""))

    Public Property Glyph As String
        Get
            Return DirectCast(GetValue(GlyphProperty), String)
        End Get
        Set(value As String)
            SetValue(GlyphProperty, value)
        End Set
    End Property

    Public Shared ReadOnly SelectedForegroundProperty As DependencyProperty = DependencyProperty.Register("SelectedForeground",
                           GetType(Brush), GetType(HoverButton), New PropertyMetadata(New SolidColorBrush()))

    Public Property SelectedForeground As Brush
        Get
            Return DirectCast(GetValue(SelectedForegroundProperty), Brush)
        End Get
        Set(value As Brush)
            SetValue(SelectedForegroundProperty, value)
        End Set
    End Property

    Public Shared ReadOnly ForegroundBrushProperty As DependencyProperty = DependencyProperty.Register("ForegroundBrush",
                           GetType(Brush), GetType(HoverButton), New PropertyMetadata(New SolidColorBrush()))

    Public Property ForegroundBrush As Brush
        Get
            Return DirectCast(GetValue(ForegroundBrushProperty), Brush)
        End Get
        Set(value As Brush)
            SetValue(ForegroundBrushProperty, value)
        End Set
    End Property

    Public Shared ReadOnly PressedForegroundProperty As DependencyProperty = DependencyProperty.Register("SelectedForeground",
                           GetType(Brush), GetType(HoverButton), New PropertyMetadata(New SolidColorBrush()))

    Public Property PressedForeground As Brush
        Get
            Return DirectCast(GetValue(PressedForegroundProperty), Brush)
        End Get
        Set(value As Brush)
            SetValue(PressedForegroundProperty, value)
        End Set
    End Property


    Public Shared ReadOnly CommandProperty As DependencyProperty = DependencyProperty.Register("Command",
                           GetType(RelayCommand), GetType(HoverButton), New PropertyMetadata(Nothing))

    Public Property Command As RelayCommand
        Get
            Return DirectCast(GetValue(CommandProperty), RelayCommand)
        End Get
        Set(value As RelayCommand)
            SetValue(CommandProperty, value)
        End Set
    End Property

    Public Shared ReadOnly CommandParameterProperty As DependencyProperty = DependencyProperty.Register("Command",
                           GetType(Object), GetType(HoverButton), New PropertyMetadata(Nothing))

    Public Property CommandParameter As Object
        Get
            Return GetValue(CommandParameterProperty)
        End Get
        Set(value As Object)
            SetValue(CommandParameterProperty, value)
        End Set
    End Property

    Private Entered As Boolean
    Private Sub FontIcon_PointerEntered(sender As Object, e As PointerRoutedEventArgs)
        Foreground = SelectedForeground
        Entered = True
    End Sub

    Private Sub FontIcon_PointerExited(sender As Object, e As PointerRoutedEventArgs)
        Foreground = ForegroundBrush
        Entered = False
    End Sub

    Private Sub FontIcon_PointerPressed(sender As Object, e As PointerRoutedEventArgs)
        If Command IsNot Nothing Then
            Command.Execute(CommandParameter)
        End If
        Foreground = PressedForeground
        e.Handled = True
    End Sub

    Private Sub FontIcon_PointerReleased(sender As Object, e As PointerRoutedEventArgs)
        Foreground = If(Entered, SelectedForeground, ForegroundBrush)
    End Sub
End Class

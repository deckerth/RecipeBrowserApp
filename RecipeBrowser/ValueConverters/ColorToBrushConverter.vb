Imports Windows.UI

Namespace Global.RecipeBrowser.ValueConverters

    Public Class ColorToBrushConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.Convert
            Dim result As SolidColorBrush = Nothing
            If value IsNot Nothing Then
                result = New SolidColorBrush(DirectCast(value, Color))
            End If
            Return result
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace

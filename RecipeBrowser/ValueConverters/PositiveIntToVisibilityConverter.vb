Namespace Global.RecipeBrowser.ValueConverters

    Public Class PositiveIntToVisibilityConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.Convert
            Dim vis As Visibility = Visibility.Collapsed
            If value IsNot Nothing AndAlso value > 0 Then
                vis = Visibility.Visible
            End If
            Return vis
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, language As String) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace

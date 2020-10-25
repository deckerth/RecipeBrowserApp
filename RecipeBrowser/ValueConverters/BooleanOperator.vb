Namespace Global.RecipeBrowser.ValueConverters

    Public Class BooleanOperator

        Public Shared Function OpAnd(a As Boolean, b As Boolean) As Boolean
            Return a AndAlso b
        End Function

        Public Shared Function OpAndNot(a As Boolean, b As Boolean) As Boolean
            Return a AndAlso Not b
        End Function

        Public Shared Function OpOr(a As Boolean, b As Boolean) As Boolean
            Return a OrElse b
        End Function

        Public Shared Function OpNot(a As Boolean) As Boolean
            Return Not a
        End Function

        Public Shared Function OpNotNullAndNot(a As Object, b As Boolean) As Boolean
            Return a IsNot Nothing AndAlso Not b
        End Function

        Public Shared Function VisAnd(a As Boolean, b As Boolean) As Visibility
            If a AndAlso b Then
                Return Visibility.Visible
            Else
                Return Visibility.Collapsed
            End If
        End Function

        Public Shared Function VisAndNot(a As Boolean, b As Boolean) As Visibility
            If a AndAlso Not b Then
                Return Visibility.Visible
            Else
                Return Visibility.Collapsed
            End If
        End Function

        Public Shared Function VisOr(a As Boolean, b As Boolean) As Visibility
            If a OrElse b Then
                Return Visibility.Visible
            Else
                Return Visibility.Collapsed
            End If
        End Function

        Public Shared Function VisNot(a As Boolean) As Visibility
            If Not a Then
                Return Visibility.Visible
            Else
                Return Visibility.Collapsed
            End If
        End Function

        Public Shared Function VisNotNullAndNot(a As Object, b As Boolean) As Visibility
            If a IsNot Nothing AndAlso Not b Then
                Return Visibility.Visible
            Else
                Return Visibility.Collapsed
            End If
        End Function

    End Class

End Namespace

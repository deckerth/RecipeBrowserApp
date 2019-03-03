Namespace Global.RecipeBrowser.Controls

    Public Class WrapPanel
        Inherits Panel

        Protected Overrides Function MeasureOverride(availableSize As Size) As Size

            ' Just take up all of the width
            Dim finalSize As New Size With {.Width = availableSize.Width}

            Dim x As Double = 0
            Dim rowHeight As Double = 0D

            For Each child In Children
                ' Tell the child control to determine the size needed
                child.Measure(availableSize)

                ' adjust the height of the panel
                x = x + child.DesiredSize.Width

                If x > availableSize.Width Then
                    ' this item will start the next row
                    x = child.DesiredSize.Width

                    ' adjust the height of the panel
                    finalSize.Height = finalSize.Height + rowHeight

                    rowHeight = child.DesiredSize.Height
                Else
                    ' Get the tallest item
                    rowHeight = Math.Max(child.DesiredSize.Height, rowHeight)
                End If
            Next

            ' Add the final height
            finalSize.Height = finalSize.Height + rowHeight

            Return finalSize
        End Function

        Protected Overrides Function ArrangeOverride(finalSize As Size) As Size

            Dim finalRect As New Rect(0, 0, finalSize.Width, finalSize.Height)
            Dim rowHeight As Double = 0

            For Each child In Children
                If child.DesiredSize.Width + finalRect.X > finalSize.Width Then
                    ' next row!
                    finalRect.X = 0
                    finalRect.Y = finalRect.Y + rowHeight
                    rowHeight = 0
                End If

                ' Place the item
                child.Arrange(New Rect(finalRect.X, finalRect.Y, child.DesiredSize.Width, child.DesiredSize.Height))

                ' adjust the location for the next items
                finalRect.X = finalRect.X + child.DesiredSize.Width
                rowHeight = Math.Max(child.DesiredSize.Height, rowHeight)
            Next

            Return finalSize
        End Function

    End Class

End Namespace

Namespace Global.Timers

    Public Class Templates

        Private CurrentTimer As Integer
        Private Sub SetTimer_Click(sender As Object, e As RoutedEventArgs)
            CurrentTimer = DirectCast(sender, AppBarButton).Tag
            Timers.Factory.Current.StopRinger()
        End Sub

        Private Sub StartTimer_Click(sender As Object, e As RoutedEventArgs)
            CurrentTimer = DirectCast(sender, AppBarButton).Tag
            Timers.Factory.Current.Timers(CurrentTimer).StartTimer()
            Timers.Factory.Current.StopRinger()
        End Sub

        Private Sub StopTimer_Click(sender As Object, e As RoutedEventArgs)
            CurrentTimer = DirectCast(sender, AppBarButton).Tag
            Timers.Factory.Current.Timers(CurrentTimer).StopTimer()
            Timers.Factory.Current.StopRinger()
        End Sub

        Private Sub PauseTimer_Click(sender As Object, e As RoutedEventArgs)
            CurrentTimer = DirectCast(sender, AppBarButton).Tag
            Timers.Factory.Current.Timers(CurrentTimer).PauseTimer()
            Timers.Factory.Current.StopRinger()
        End Sub

        Private Sub SetTimer_TimePicked(sender As TimePickerFlyout, args As TimePickedEventArgs)
            Timers.Factory.Current.Timers(CurrentTimer).SetTimer(args.NewTime.TotalSeconds)
        End Sub

        Private Sub SetTimer_PointerEntered(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        End Sub

        Private Sub SetTimer_PointerExited(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        End Sub

        Private Sub StartTimer_PointerEntered(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        End Sub

        Private Sub StartTimer_PointerExited(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        End Sub

        Private Sub ContinueTimer_PointerEntered(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        End Sub

        Private Sub ContinueTimer_PointerExited(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        End Sub

        Private Sub PauseTimer_PointerEntered(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        End Sub

        Private Sub PauseTimer_PointerExited(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        End Sub

        Private Sub StopTimer_PointerEntered(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraBlack
        End Sub

        Private Sub StopTimer_PointerExited(sender As Object, e As PointerRoutedEventArgs)
            sender.FontWeight = Windows.UI.Text.FontWeights.ExtraLight
        End Sub
    End Class

End Namespace


Imports Windows.Storage

Namespace Global.RtfEditor

    Public Class FavoriteFonts


        Private Shared _Current As FavoriteFonts
        Public Shared ReadOnly Property Current As FavoriteFonts
            Get
                If _Current Is Nothing Then
                    _Current = New FavoriteFonts
                End If
                Return _Current
            End Get
        End Property

        Private _LastUsedFonts As List(Of String)
        Public ReadOnly Property LastUsedFonts As List(Of String)
            Get
                If _LastUsedFonts Is Nothing Then
                    LoadFavorites()
                End If
                Return _LastUsedFonts
            End Get
        End Property

        Private Sub LoadFavorites()

            If _LastUsedFonts IsNot Nothing Then
                Return
            End If

            Dim roamingSettings = Windows.Storage.ApplicationData.Current.LocalSettings
            Dim fontList = roamingSettings.CreateContainer("LastUsedFonts", Windows.Storage.ApplicationDataCreateDisposition.Always)

            _LastUsedFonts = New List(Of String)

            For Each item In fontList.Values
                Dim fontComposite As ApplicationDataCompositeValue = item.Value
                Dim fontFamily As String
                Try
                    fontFamily = fontComposite("FontFamily")
                Catch ex As Exception
                End Try
                If fontFamily IsNot Nothing Then
                    _LastUsedFonts.Add(fontFamily)
                End If
            Next

        End Sub

        Public Sub AddFont(ByRef newFont As String)

            Dim index As Integer = 0
            For Each f In LastUsedFonts
                If f.Equals(newFont) Then
                    Exit For
                End If
                index = index + 1
            Next

            If index >= LastUsedFonts.Count Then
                While LastUsedFonts.Count >= 4
                    'Remove last
                    _LastUsedFonts.RemoveAt(LastUsedFonts.Count - 1)
                End While
            ElseIf index = 0 Then
                Return
            Else
                'Move to top of list
                _LastUsedFonts.RemoveAt(index)
            End If
            _LastUsedFonts.Insert(0, newFont)
            SaveFavorites()
        End Sub

        Private Sub SaveFavorites()
            Dim roamingSettings = Windows.Storage.ApplicationData.Current.LocalSettings
            Dim fontList = roamingSettings.CreateContainer("LastUsedFonts", Windows.Storage.ApplicationDataCreateDisposition.Always)
            fontList.Values.Clear()
            Dim index As Integer = 0
            For Each f In _LastUsedFonts
                Dim fontComposite = New Windows.Storage.ApplicationDataCompositeValue()
                fontComposite("FontFamily") = f
                fontList.Values(index.ToString) = fontComposite
                index = index + 1
            Next
        End Sub

    End Class

End Namespace

Imports Windows.Storage

Public Class AssetAccess

    Public Shared Async Function CopyAssetFile(ByVal assetDir As StorageFolder,
                                                ByVal templateFolder As StorageFolder,
                                                ByVal fileName As String,
                                                Optional desiredFilename As String = Nothing) As Task

        Dim template As StorageFile
        Dim copiedFile As StorageFile

        Try
            template = Await assetDir.GetFileAsync(fileName)
            If template IsNot Nothing Then
                copiedFile = Await template.CopyAsync(templateFolder)
                If desiredFilename IsNot Nothing Then
                    Await copiedFile.RenameAsync(desiredFilename, NameCollisionOption.ReplaceExisting)
                End If
            End If
        Catch ex As Exception
            App.Logger.Write("Unable to access " + fileName + ":" + ex.ToString)
        End Try

    End Function

End Class

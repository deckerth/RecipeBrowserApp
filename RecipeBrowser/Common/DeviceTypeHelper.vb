Imports Windows.System.Profile

Namespace Common

    Public Class DeviceTypeHelper

        Public Enum DeviceFormFactorType
            Phone
            Desktop
            Tablet
            IoT
            SurfaceHub
            Other
        End Enum

        Public Shared Function GetDeviceFormFactorType() As DeviceFormFactorType
            Select Case AnalyticsInfo.VersionInfo.DeviceFamily
                Case "Windows.Mobile"
                    Return DeviceFormFactorType.Phone
                Case "Windows.Desktop"
                    If UIViewSettings.GetForCurrentView().UserInteractionMode = UserInteractionMode.Mouse Then
                        Return DeviceFormFactorType.Desktop
                    Else
                        Return DeviceFormFactorType.Tablet
                    End If
                Case "Windows.Universal"
                    Return DeviceFormFactorType.IoT
                Case "Windows.Team"
                    Return DeviceFormFactorType.SurfaceHub
                Case Else
                    Return DeviceFormFactorType.Other
            End Select
        End Function

    End Class

End Namespace

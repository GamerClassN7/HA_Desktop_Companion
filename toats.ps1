$headlineText = 'One string of bold text on the first line.'
$bodyText = 'One string of regular text on the second line.'
$logo = 'C:\Windows\IdentityCRL\WLive48x48.png'
$image = 'C:\Windows\Web\Screen\img100.jpg'

$xml = @"
<toast>
    <visual>
        <binding template="ToastGeneric">
            <text>$($headlineText)</text>
            <text>$($bodyText)</text>
            <image placement="appLogoOverride" src="$($logo)"/>
            <image src="$($image)"/>
        </binding>
    </visual>
</toast>
"@
$XmlDocument = [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime]::New()
$XmlDocument.loadXml($xml)
$AppId = '{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe'
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime]::CreateToastNotifier($AppId).Show($XmlDocument)
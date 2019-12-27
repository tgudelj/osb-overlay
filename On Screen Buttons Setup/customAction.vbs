rem MsgBox "Custom action invoked ->" + Session.Property("CustomActionData")
VJoyPath = Session.Property("CustomActionData") & "\vJoySetup.exe" 
Set WshShell = CreateObject( "WScript.Shell" )
WshShell.Run ("cmd /C " & """" & vJoyPath & """")
Set WshShell = Nothing
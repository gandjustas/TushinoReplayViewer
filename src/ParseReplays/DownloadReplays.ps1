$url = "http://185.244.43.73/replays/"
$path = "F:\Arma\replays\"
$WebResponse= Invoke-WebRequest $url
foreach ($link in $WebResponse.Links) 
{
    $name = $link.href
    if($name.endswith(".pbo.7z")) 
    {
        $output = $path+$name
        $file = $url+$name
        if(-not (Test-Path -Path $output)) 
        {
            Start-BitsTransfer -Source $file -Destination $output
            Write-Host "$name downloaded"
        } 
        else 
        {
            Write-Host "$name exists"
        }
    }
}
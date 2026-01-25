param(
	[string]
	$TargetName="ktwrd/xenia-discord-dash",
	[string]
	$TargetTag="latest",
	[bool]
	$IncludeTimeTag=1
)
$targetTag="$($TargetName):$($TargetTag)"
docker build -t xenia-webpanel:latest -f WebPanel.Dockerfile .

docker tag xenia-webpanel:latest $targetTag
docker push $targetTag

if ($IncludeTimeTag -eq $True)
{
	$currentTimeTag=@(Get-Date -UFormat %s -Millisecond 0)
	$timeTag="$($TargetName):$($currentTimeTag)"
	docker tag xenia-webpanel:latest $timeTag
	docker push $timeTag
}

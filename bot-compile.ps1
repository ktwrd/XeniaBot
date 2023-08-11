param(
	[string]
	$TargetName="ktwrd/xenia-discord",
	[string]
	$TargetTag="latest",
	[bool]
	$IncludeTimeTag=1
)
$targetTag="$($TargetName):$($TargetTag)"
docker build -t xenia-discord:latest .

docker tag xenia-discord:latest $targetTag
docker push $targetTag

if ($IncludeTimeTag -eq $True)
{
	$currentTimeTag=@(Get-Date -UFormat %s -Millisecond 0)
	$timeTag="$($TargetName):$($currentTimeTag)"
	docker tag xenia-discord:latest $timeTag
	docker push $timeTag
}
param(
	[string]
	$TargetName="ktwrd/xenia-discord-xp",
	[string]
	$TargetTag="latest",
	[bool]
	$IncludeTimeTag=1
)
$targetTag="$($TargetName):$($TargetTag)"
docker build -t xenia-discord-levelsystem:latest -f Bot_LevelSystem.Dockerfile .

docker tag xenia-discord-levelsystem:latest $targetTag
docker push $targetTag

if ($IncludeTimeTag -eq $True)
{
	$currentTimeTag=@(Get-Date -UFormat %s -Millisecond 0)
	$timeTag="$($TargetName):$($currentTimeTag)"
	docker tag xenia-discord-levelsystem:latest $timeTag
	docker push $timeTag
}
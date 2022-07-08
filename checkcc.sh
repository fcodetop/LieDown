#!/bin/bash
export LOTUS_PATH=/mnt/md0/lotus-path
export LOTUS_MINER_PATH=/mnt/md0/lotus-miner-path
for sid in `./lotus-miner sealing jobs |grep running |grep miner |awk '{print $$2}'`;
do
	num=`/mnt/md0/miner/lotus-miner status $sid |grep "Deals:" |awk -F \[ '{print $$2}'`
	if [ $num -eq "0]" ];then		
		echo ${sid} "not a deal"
		/mnt/md0/miner/lotus-miner sealing jobs |grep sid |awk '{print $$1}' |xargs echo 
	else
		echo ${sid} ${num}
	fi	
done

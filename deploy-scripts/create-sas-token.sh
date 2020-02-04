#!/bin/bash

##########################################################################
##  Create SAS token to read artefacts
##
##  Inputs:
##      account:  Storage account name
##
##  Outputs:
##      sas:  SAS token

account=$1
expiry=$(date -u -d "15 minutes" '+%Y-%m-%dT%H:%MZ')

echo
echo "Account:  $account"
echo "Expiry:  $expiry"

echo
echo "Generate SAS Token"

sas=$(az storage container generate-sas \
    --account-name $account \
    -n artefacts \
    --https-only \
    --permissions r \
    --expiry $expiry \
    -o tsv)

echo "Generated SAS:  $sas"

#   Pass it as an output variable
echo "##vso[task.setvariable variable=sas;]$sas"
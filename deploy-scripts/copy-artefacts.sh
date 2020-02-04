#!/bin/bash

##########################################################################
##  Copy deployment files to storage account
##
##  Inputs:
##      releaseFolderUrl:  Release folder URL (in storage account)

releaseFolderUrl=$1

echo
echo "Release Folder Url:  $releaseFolderUrl"
echo "Current Folder:  $(pwd)"

echo
echo "ls -l"

ls -l

echo
echo "Copy files..."

az storage copy \
    -s templates \
    -d $releaseFolderUrl \
    --recursive

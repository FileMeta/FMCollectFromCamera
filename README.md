# FMCollectFromCamera
Collects photos, videos, and other media files from a camera memory card or other [DCF](https://en.wikipedia.org/wiki/Design_rule_for_Camera_File_system) compliant device.

This program is intended as the first step in a two-step import into [FileMeta](http://www.filemeta.org) media collection. This step collects the media files into an auto-sort directory and deletes them from the source device or memory card.

Subsequent steps are the following:

* Normalize the file formats (e.g. orient all images to vertical and convert all videos to .mp4 format)
* Fill in missing metadata (e.g. most cameras include metadata in JPEG files but leave it out of video files)
* Auto-sort the files into folders according to a defined convention based on metadata.

## Execution Notes
The destination folder path should be specified on the command line. The program will search all removable devices for a [DCF](https://en.wikipedia.org/wiki/Design_rule_for_Camera_File_system) compatible file tree and transfer the files encountered into the target directory.

## Build Notes
Written in C#. Built using Microsoft Visual Studio Ecpress 2013 for Windows Desktop. Should be compatible with other versions of Visual Studio.

/// Uploads an AI embeddings storage file to the LA website.
/// Currently used only for icons embeddings.
/// To run, click link printed by Embeddings._GetEmbeddings. Can't run directly because need a hash etc.
/// It prints the link when created new embeddings. To print always when UI-searching, temporarily enable the `//emFile.PrintUploadIfAtHome`.

/*/ c Sftp.cs; c Ed util shared.cs; /*/

string file = args[0], zipName = args[1];
//print.it(file, zipName);

if (!dialog.showOkCancel("Upload AI embedding vectors", zipName)) return;

string zipFile = folders.ThisAppTemp + zipName;
try {
	if (!LA.SevenZip.Compress(out var errors, zipFile, file)) { print.it(errors); return; }
	
	//run.selectInExplorer(zipFile);
	Sftp.UploadToLA("domains/libreautomate.com/public_html/download/ai/embedding", zipFile);
	
	print.it($"Uploaded: {file} -> https://www.libreautomate.com/download/ai/embedding/{zipName}");
}
finally { filesystem.delete(zipFile, FDFlags.CanFail); }

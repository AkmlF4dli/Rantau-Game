mergeInto(LibraryManager.library, {

  _PatchJsFilesystem: function () {
    var tmp_fs_sync = fs.sync;
    fs.sync = function(onlyPendingSync) {
      if (onlyPendingSync) return;
      tmp_fs_sync(onlyPendingSync);
     };
  }

});

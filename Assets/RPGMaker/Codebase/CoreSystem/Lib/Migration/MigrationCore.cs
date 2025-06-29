#if UNITY_EDITOR && !UNITE_WEBGL_TEST
using RPGMaker.Codebase.CoreSystem.Helper;
using RPGMaker.Codebase.CoreSystem.Lib.Migration.ExecutionClasses;
using RPGMaker.Codebase.CoreSystem.Lib.RepositoryCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RPGMaker.Codebase.CoreSystem.Lib.Migration
{
    public class MigrationCore
    {
        //----------------------------------------------------------------------------------------------------------------------------------
        // マイグレーション履歴ファイルへのパス
        private const string MigrationHistoryDir = "Assets/RPGMaker/Storage/Migration";
        private const string MigrationHistoryFile = MigrationHistoryDir + "/MigrationHistoryCore.json";

        //----------------------------------------------------------------------------------------------------------------------------------
        // 実行するマイグレーション処理クラス一覧
        // マイグレーション処理クラスが増えたらここに追加すること
        // このリスト順でマイグレーション処理が実行される
        private static readonly List<IExecutionClassCore> ExecutionClasses = new List<IExecutionClassCore>
        {
            new Migration_1_0_1_Class(),
            new Migration_1_0_2_Class(),
            new Migration_1_0_3_Class(),
            new Migration_1_0_4_Class(),
            new Migration_1_0_6_Class(),
            new Migration_1_0_7_Class(),
        };

        /**
         * マイグレーションを実行する
         */
        public static void Migrate() {

            if (!Auth.Auth.IsAuthenticated)
            {
                return;
            }

            var migrationHistoryDataModels = new List<MigrationHistoryDataModelCore>();

            try
            {
                // マイグレーション履歴ファイルが存在しない場合は新規作成
                if (!Directory.Exists(MigrationHistoryDir))
                {
                    Directory.CreateDirectory(MigrationHistoryDir);
                }
                if (!File.Exists(MigrationHistoryFile))
                {
                    File.WriteAllText(MigrationHistoryFile, "[]");
                }
                // マイグレーション履歴を取得
                {
                    var fs = new FileStream(MigrationHistoryFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs, Encoding.GetEncoding("UTF-8"));
                    migrationHistoryDataModels = JsonHelperForRepositoryCore.FromJsonArray<MigrationHistoryDataModelCore>(sr.ReadToEnd());
                }
            }
            catch (Exception)
            {
                // マイグレーション履歴を正常に取得できなかった場合はエラーを出力し処理を中断する
                return;
            }

            // マイグレーション処理をどこからやるかを取得
            //・実行した記録がある場合はそのバージョンの次から
            //・実行した記録がない場合はすべて
            var lastModel = migrationHistoryDataModels.LastOrDefault();
            var targetIdentifire = lastModel == default ? "" : lastModel.executionClassIdentifier;
            var doMigration = lastModel == default ? true : false;
            var unCompressedZip = false;

            AssetDatabase.StartAssetEditing();

            foreach (var execution in ExecutionClasses)
            {
                if (doMigration)
                {
                    try
                    {
                        // マイグレーション処理実行
                        execution.Execute();

                        //Storageのマイグレーション処理実行
                        //Storageの一時解凍が必要なマイグレーションが含まれている場合には、ユーザーフォルダ下に一時的に展開する
                        if (execution.IsStorageUpdate())
                        {
                            if (unCompressedZip == false)
                            {
                                unCompressedZip = true;
                                UncompressStorage();
                            }
                            OverWriteProjectStorageFile(execution.ListStorageCopy());
                            DeleteProjectStorageFile(execution.ListStorageDelete());
                        }
                        {
                            migrationHistoryDataModels.Add(
                                new MigrationHistoryDataModelCore
                                {
                                    id = Guid.NewGuid().ToString(),
                                    executionClassIdentifier = execution.GetIdentifier(),
                                    executedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                }
                            );
                            File.WriteAllText(MigrationHistoryFile, JsonHelperForRepositoryCore.ToJsonArray(migrationHistoryDataModels));
                        }
                    }
                    catch (Exception e)
                    {
                        //・マイグレーション処理に失敗した場合
                        //・マイグレーション履歴作成に失敗した場合
                        // 処理をロールバックして抜ける
                        execution.Rollback();
                        break;
                    }
                }
                else
                {
                    // バージョンをチェックして、この次のバージョン情報からマイグレーションを実行
                    if (execution.GetIdentifier() == targetIdentifire)
                    {
                        doMigration = true;
                    }
                }
            }
            {
                //Migrationディレクトリが残っている場合には削除する
                var migrationPath = Path.Combine(PathManager.GetRPGMakerPersistentFolderPath(), "Migration");
                if (Directory.Exists(migrationPath))
                {
                    Directory.Delete(migrationPath, true);
                }
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }



        /**
         * マイグレーション用にStorageを一時的に解凍する
         */
        private static void UncompressStorage() {
            var folderPath = PathManager.GetRPGMakerPersistentFolderPath();
            var migrationPath = Path.Combine(folderPath, "Migration");

            if (!Directory.Exists(migrationPath))
            {
                Directory.CreateDirectory(migrationPath);
            }
            //バージョン情報を、現在のプロジェクトのバージョンとする
            var currentVersion = "1.0.0";
            var LocalVersionPath = Path.Combine(Application.dataPath, "../Packages/jp.ggg.rpgmaker.unite/version.txt");
            if (File.Exists(LocalVersionPath))
            {
                using var fs = new FileStream(LocalVersionPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                currentVersion = reader.ReadToEnd();
            }

            var commonZipFilePath = Path.Combine(folderPath, $"{currentVersion}/defaultgame_jp_v{currentVersion}.zip");
            if (!File.Exists(commonZipFilePath))
            {
                throw new Exception("Package file could not be found.");
            }
            //指定されたフォルダ内の、Storage領域に、共通Storageを解凍する
            ZipFile.ExtractToDirectory(commonZipFilePath, migrationPath, true);

            //言語ごとに異なるファイルについては、ファイル置換によるマイグレーションは不可で、個別処理が必要
        }

        private static void OverWriteProjectStorageFile(List<string> targetFileNameList) {
            if (targetFileNameList == null)
            {
                return;
            }
            var sourceFolderPath = Path.Combine(PathManager.GetRPGMakerPersistentFolderPath(), "Migration");
            var storageRootPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");
            targetFileNameList.ForEach(element =>
            {
                var sourceFilePath = Path.Combine(sourceFolderPath, element);
                var destFilePath = Path.Combine(storageRootPath, element);
                if (File.Exists(sourceFilePath))
                {
                    File.Copy(sourceFilePath, destFilePath, true);
                }
            });
        }

        private static void DeleteProjectStorageFile(List<string> targetFileNameList) {
            if (targetFileNameList == null)
            {
                return;
            }
            var storageRootPath = Path.Combine(Application.dataPath, "RPGMaker", "Storage");
            targetFileNameList.ForEach(element =>
            {
                var targetFilePath = Path.Combine(storageRootPath, element);
                if (File.Exists(targetFilePath))
                {
                    File.Delete(targetFilePath);
                }
            });
        }
    }
}

#endif
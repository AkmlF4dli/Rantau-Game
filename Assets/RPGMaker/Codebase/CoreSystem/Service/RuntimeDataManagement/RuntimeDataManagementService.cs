using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.CoreSystem.Knowledge.Misc;
using RPGMaker.Codebase.CoreSystem.Service.DatabaseManagement.Repository;
using RPGMaker.Codebase.CoreSystem.Service.RuntimeDataManagement.Repository;
using System.Threading.Tasks;

namespace RPGMaker.Codebase.CoreSystem.Service.RuntimeDataManagement
{
    public class RuntimeDataManagementService
    {
        private readonly NewGameRepository _newGameRepository;
        private readonly TitleRepository   _titleRepository;

        public RuntimeDataManagementService() {
            _newGameRepository = new NewGameRepository();
            _titleRepository = new TitleRepository();
        }

        public RuntimeConfigDataModel LoadConfig() {
            return _newGameRepository.LoadConfig();
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public RuntimeSaveDataModel LoadSaveData(int id) {
            return _newGameRepository.LoadData(id);
        }
#else
        public async Task<RuntimeSaveDataModel> LoadSaveData(int id) {
            return await _newGameRepository.LoadData(id);
        }
#endif

        public RuntimeConfigDataModel NewConfig() {
            return _newGameRepository.NewConfig();
        }

        public void SaveConfig() {
            _newGameRepository.SaveConfig();
        }

        /// <summary>
        ///     ゲームデータのセーブの実施
        /// </summary>
        /// <param name="runtimeSaveDataModel">参照するセーブデータ</param>
        public void SaveSaveData(RuntimeSaveDataModel runtimeSaveDataModel, int id) {
            _newGameRepository.SaveData(runtimeSaveDataModel, id);
        }

        /// <summary>
        ///     ゲームデータのオートセーブの実施
        /// </summary>
        /// <param name="runtimeSaveDataModel">参照するセーブデータ</param>
        public void SaveAutoSaveData(RuntimeSaveDataModel runtimeSaveDataModel) {
            _newGameRepository.SaveData(runtimeSaveDataModel, 0);
        }

#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public void StartNewGame(BattleSceneTransition battleTest = null) {
            _newGameRepository.CreateGame(battleTest);
        }
#else
        public async Task StartNewGame(BattleSceneTransition battleTest = null) {
            await _newGameRepository.CreateGame(battleTest);
        }
#endif

        public RuntimeSaveDataModel StartLoadGame() {
            return _newGameRepository.CreateLoadGame();
        }

        /// <summary>
        /// ActorData生成
        /// </summary>
        /// <param name="actorData"></param>
        /// <returns></returns>
#if (UNITY_EDITOR && !UNITE_WEBGL_TEST) || !UNITY_WEBGL
        public RuntimeActorDataModel CreateActorData(BattleSceneTransition.Actor actorData) {
            return _newGameRepository.CreateActorData(actorData);
        }
#else
        public async Task<RuntimeActorDataModel> CreateActorData(BattleSceneTransition.Actor actorData) {
            return await _newGameRepository.CreateActorData(actorData);
        }
#endif

        public int GetSaveFileCount() {
            return _newGameRepository.GetSaveFileCount();
        }

        public bool IsAutoSaveFile() {
            return _newGameRepository.IsAutoSaveFile();
        }

        public string LoadSaveData(string filename) {
            return _newGameRepository.LoadSaveFile(filename);
        }
    }
}
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Common;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Event;
using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Runtime;
using RPGMaker.Codebase.Runtime.Common;

namespace RPGMaker.Codebase.Runtime.Event.AudioVideo
{
    /// <summary>
    /// [オーディオビデオ]-[BGMの演奏]
    /// </summary>
    public class BgmPlayProcessor : AbstractEventCommandProcessor
    {
        protected override void Process(string eventID, EventDataModel.EventCommand command) {
            var soundName = command.parameters[0];
            if (!SoundManager.Self().IsBgmPlaying(soundName))
            {
                SoundManager.Self().Init();

                //サウンドデータの生成(曲名、位相、ピッチ、ボリューム)
                var sound = new SoundCommonDataModel(
                    soundName,
                    int.Parse(command.parameters[3]),
                    int.Parse(command.parameters[2]),
                    int.Parse(command.parameters[1]));

                //データのセット
                SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_BGM, sound);

                //BGM再生
                PlaySound();
                return;
            }
            else
            {
                //U342 再生中の同名BGMの場合に、音量のみ設定を変更する
                var BgmSound = SoundManager.Self().GetBgmSound();
                if (BgmSound != null)
                {
                    int Volume = int.Parse(command.parameters[1]);
                    var runtimeConfigDataModel = DataManager.Self().GetRuntimeConfigDataModel();
                    if (BgmSound.volume != Volume)
                    {
                        //サウンドデータの生成(曲名、位相、ピッチ、ボリューム)
                        var sound = new SoundCommonDataModel(
                            soundName,
                            int.Parse(command.parameters[3]),
                            int.Parse(command.parameters[2]),
                            int.Parse(command.parameters[1]));
                        //データのセット
                        SoundManager.Self().SetData(SoundManager.SYSTEM_AUDIO_BGM, sound);
                        // 音量のみ設定反映
                        SoundManager.Self().ChangeBgmState(runtimeConfigDataModel.bgmVolume);
                    }
                }
            }

            //次のイベントへ
            ProcessEndAction();
        }

        private async void PlaySound() {
            //サウンドの再生
            await SoundManager.Self().PlayBgm();

            //次のイベントへ
            ProcessEndAction();
        }

        private void ProcessEndAction() {
            SendBackToLauncher.Invoke();
        }
    }
}
using RPGMaker.Codebase.CoreSystem.Knowledge.Enum;
using RPGMaker.Codebase.Runtime.Map;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RPGMaker.Codebase.Runtime.Common
{
    /// <summary>
    /// Runtime, Editorのプレビュー, EditorのUIで共用するもの。
    /// </summary>
    public static class Commons
    {
        public const int Fps = 60;

        public static CharacterMoveDirectionEnum GetDirection(Vector2 fromPosition, Vector2 toPosition)
        {
            return GetDirection(toPosition - fromPosition);
        }

        public static CharacterMoveDirectionEnum GetDirection(Vector2 vector)
        {
            return
                vector == Vector2.zero ?
                    CharacterMoveDirectionEnum.None :
                    Mathf.Abs(vector.x) >= Mathf.Abs(vector.y) ?
                        (vector.x >= 0f ? CharacterMoveDirectionEnum.Right : CharacterMoveDirectionEnum.Left) :
                        (vector.y >= 0f ? CharacterMoveDirectionEnum.Up : CharacterMoveDirectionEnum.Down);
        }

        /// <summary>
        /// 速度倍数を扱うクラス。
        /// </summary>
        /// <remarks>
        /// イベントコマンド『移動ルートの指定』『ジャンプ』で使用する (他でも共用したほうが良いもの (プレビュー含む))。
        /// </remarks>
        public static class SpeedMultiple
        {
            /// <summary>
            /// 速度倍数id。
            /// </summary>
            public enum Id
            {
                Divide8,
                Divide4,
                Divide2,
                Multiply1,
                Multiply2,
                Multiply4,
            }

            /// <summary>
            /// 速度倍数値を取得。
            /// </summary>
            public static float GetValue(Id id)
            {
                return id switch
                {
                    Id.Divide8 => 1 / 8f,
                    Id.Divide4 => 1 / 4f,
                    Id.Divide2 => 1 / 2f,
                    Id.Multiply1 => 1f,
                    Id.Multiply2 => 2f,
                    Id.Multiply4 => 4f,
                    _ => 0f,
                };
            }
        }

        /// <summary>
        /// 向きを扱うクラス。
        /// </summary>
        /// <remarks>
        /// イベントコマンド『移動ルートの指定』『ジャンプ』で使用する (他でも共用したほうが良いもの (プレビュー含む))。
        /// </remarks>
        public static class Direction
        {
            /// <summary>
            /// 向きid。
            /// </summary>
            public enum Id : int
            {
                NowDirection,
                Player,
                Down,
                Left,
                Right,
                Up,
                Damage
            }

            /// <summary>
            /// 向きを取得。
            /// </summary>
            public static CharacterMoveDirectionEnum GetCharacterMoveDirection(
                Id directionId, GameObject fromGameObject, GameObject toGameObject)
            {
                return directionId switch
                {
                    Id.Player => GetDirectionToPlayer(),
                    Id.Down => CharacterMoveDirectionEnum.Down,
                    Id.Left => CharacterMoveDirectionEnum.Left,
                    Id.Right => CharacterMoveDirectionEnum.Right,
                    Id.Up => CharacterMoveDirectionEnum.Up,
                    Id.Damage => CharacterMoveDirectionEnum.Damage,
                    _ => CharacterMoveDirectionEnum.None

                };

                CharacterMoveDirectionEnum GetDirectionToPlayer()
                {
                    if (fromGameObject == null || toGameObject == null)
                    {
                        return CharacterMoveDirectionEnum.None;
                    }

                    return GetDirection(fromGameObject.transform.position, toGameObject.transform.position);
                }
            }
        }

        /// <summary>
        /// 対象キャラクターを示すクラス。
        /// </summary>
        /// <remarks>
        /// 以下のいずれかを示す。
        /// ・プレイヤー (プレイヤーキャラクター)。
        /// ・このイベント (このイベントのキャラクター)。
        /// ・指定のイベント (イベントが存在するマップに存在するイベントの内の指定のイベントのキャラクター)。
        /// 
        /// 以下のイベントコマンドで使用する。
        /// －－－－－－－－－－－－－－－－－－－－－－－－－－－－－－－－－－
        /// イベントコマンド分類　　　　　イベントコマンド名　　　対象選択項目名
        /// －－－－－－－－－－－－－－　－－－－－－－－－－－　－－－－－－－
        /// キャラクター                  アニメーションの表示    キャラクター
        /// キャラクター                  フキダシアイコンの表示  キャラクター
        /// キャラクター                  向き                    キャラクター
        /// キャラクター                  アニメーション指定      キャラクター
        /// キャラクター                  キャラ画像設定          イベント
        /// キャラクター　＞　移動        場所移動                ※なし (参考用)
        /// キャラクター　＞　移動        移動ルートの指定        イベント指定
        /// キャラクター　＞　アクション  ジャンプ                イベント指定
        /// マップ                        指定位置の情報取得      キャラクターで指定
        /// ゲーム進行                    変数の操作              操作　＞　ゲームデータ　＞　キャラクター
        /// フロー制御                    分岐設定                操作　＞　キャラクター　＞　キャラクター
        /// イベント                      イベントの位置指定      イベントリスト、
        ///                                                       他のイベントと交換　＞　イベントリスト
        /// </remarks>
        public class TargetCharacter
        {
            private readonly string targetCharacterId;
            private readonly string thisEventId;

            public TargetCharacter(string targetCharacterId, string thisEventId = null)
            {
                this.targetCharacterId = targetCharacterId;
                this.thisEventId = thisEventId;
                CoreSystem.Helper.DebugUtil.Assert(!(TargetType == TargetType.ThisEvent && thisEventId == null));
            }

            public TargetCharacter(TargetType targetType, string thisEventId)
                : this(targetType.GetTargetCharacterId(), thisEventId)
            {
            }

            public TargetType TargetType => GetTargetType(targetCharacterId);
            public bool IsPlayer => TargetType == TargetType.Player;

            public string TargetEventId
            {
                get
                {
                    CoreSystem.Helper.DebugUtil.Assert(TargetType == TargetType.OtherEvent);
                    return targetCharacterId;
                }
            }

            private static TargetType GetTargetType(string targetCharacterId)
            {
                return ((TargetType[])System.Enum.GetValues(typeof(TargetType))).
                    SingleOrDefault(id => $"{(int)id}" == targetCharacterId);
            }

            public GameObject GetGameObject()
            {
                return
                    TargetType == TargetType.Player ?
                        MapManager.GetOperatingCharacterGameObject() :
                        MapEventExecutionController.Instance.
                            GetEventMapGameObject(
                                TargetType == TargetType.ThisEvent ? thisEventId : targetCharacterId);
            }

            public Vector2Int GetTilePositionOnTile()
            {
                Vector2 position =
                    TargetType == TargetType.Player ?
                        MapManager.OperatingCharacter.GetCurrentPositionOnTile() :
                        MapEventExecutionController.Instance.
                            GetEventOnMap(TargetType == TargetType.ThisEvent ? thisEventId : targetCharacterId).
                            GetCurrentPositionOnTile();
                return new Vector2Int((int)position.x, (int)position.y);
            }

            public string GetTargetCharacterId()
            {
                return TargetType.GetTargetCharacterId();
            }

            public string GetEventId(bool ifPlayerIsThisEventId = false)
            {
                CoreSystem.Helper.DebugUtil.Assert(
                    !(TargetType == TargetType.Player && ifPlayerIsThisEventId && thisEventId == null));

                return TargetType switch
                {
                    TargetType.Player => ifPlayerIsThisEventId ? thisEventId : null,
                    TargetType.ThisEvent => thisEventId,
                    _ => targetCharacterId
                };
            }
        }

        public enum TargetType
        {
            Player = -2,
            ThisEvent = -1,
            OtherEvent = 0
        }

        public static string GetTargetCharacterId(this TargetType targetType)
        {
            CoreSystem.Helper.DebugUtil.Assert(targetType != TargetType.OtherEvent);
            return $"{(int)targetType}";
        }

        public static bool IsURP() {
#if UNITY_2022_1_OR_NEWER
            return (GraphicsSettings.defaultRenderPipeline != null);
#else
            return (GraphicsSettings.renderPipelineAsset != null);
#endif
        }

        public const int DefaultRendererDataOffset = 0;
        public const int UniteRendererDataOffset = 1;

        public const int AdditionalFeatureInsertBottomOffset = 2;
        public const int EffekseerFeatureBottomOffset = 2;
        public const int MenuBlurFeatureBottomOffset = 1;

        public static (ScriptableRendererData, List<ScriptableRendererFeature>) GetUniteRendererDataFeatues() {
            try
            {
                if (!IsURP()) return (null, null);
#if UNITY_2022_1_OR_NEWER
                var urpAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
#else
                var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
#endif
                var rendererDataField = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
                //Debug.Log($"rendererDataField: {rendererDataField}");
                var rendererDataList = rendererDataField.GetValue(urpAsset) as ScriptableRendererData[];
                //Debug.Log($"rendererDataList: {rendererDataList}, {rendererDataList?.Length}");
                // 0: DefaultRendererData, 1: UniteRendererData
                if (rendererDataList.Length > UniteRendererDataOffset)
                {
                    var rendererData = rendererDataList[UniteRendererDataOffset];
                    //Debug.Log($"rendererData: {rendererData}");
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var rendererFeaturesField = typeof(ScriptableRendererData).GetField("m_RendererFeatures", flags);
                    //Debug.Log($"rendererFeaturesField: {rendererFeaturesField}");
                    var rendererFeatures = rendererFeaturesField.GetValue(rendererData) as List<ScriptableRendererFeature>;
                    //Debug.Log($"rendererFeatures: {rendererFeatures}");
                    return (rendererData, rendererFeatures);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            return (null, null);
        }
    }
}

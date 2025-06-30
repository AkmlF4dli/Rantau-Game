using RPGMaker.Codebase.CoreSystem.Knowledge.DataModel.Vehicle;
using RPGMaker.Codebase.Runtime.Common;
using System;
using UnityEngine;

namespace RPGMaker.Codebase.Runtime.Map.Component.Character
{
    public class VehicleOnMap : CharacterOnMap
    {
        // データプロパティ
        //--------------------------------------------------------------------------------------------------------------
        private VehiclesDataModel _vehiclesDataModelCache;

        // 状態プロパティ
        //--------------------------------------------------------------------------------------------------------------
        protected SpriteRenderer _spriteRenderer;

        // 表示要素プロパティ
        //--------------------------------------------------------------------------------------------------------------
        protected VehicleGraphic _vehicleGraphic;

        // 関数プロパティ
        //--------------------------------------------------------------------------------------------------------------

        protected VehiclesDataModel _vehiclesDataModel
        {
            get
            {
                return _vehiclesDataModelCache ??= DataManager.Self().GetVehicleDataModel(CharacterId);
            }
        }
        
        // initialize methods
        //--------------------------------------------------------------------------------------------------------------
        /**
         * 目的位置へ毎フレーム移動していく
         */
        override protected void MoveToPositionOnTileByFrame(Vector2 destinationPositionOnWorld, Action callBack) {
            Vector2 currentPos = gameObject.transform.position;

            //距離の長さで判定する様に変更
            var Len = (destinationPositionOnWorld - currentPos);
            var moveDelta = Time.deltaTime * _moveSpeed;
            if (Len.magnitude > moveDelta)
            {
                //メニュー開き中は進まないようにしておく
                if (MenuManager.IsMenuActive) return;

                var newPos = Vector2.MoveTowards(
                    currentPos,
                    destinationPositionOnWorld,
                    moveDelta
                   );

                SetGameObjectPositionWithRenderingOrder(newPos);
            }
            else
            {
                //ループ設定
                if (IsLoop)
                {
                    //ループする場合に最終地点についた場合に、座標をループ地点に飛ばす
                    destinationPositionOnWorld = _LoopPos;
                }
                SetCurrentPositionOnTile(destinationPositionOnWorld);
                SetToPositionOnTile(destinationPositionOnWorld);
                //ループ位置に座標を飛ばす
                SetGameObjectPositionWithRenderingOrder(destinationPositionOnWorld);
                callBack();
            }
        }

        public SpriteRenderer GetVehicleShadow() {
            return _spriteRenderer;
        }

        public void SetVehicleShadow(SpriteRenderer spriteRenderer) {
            _spriteRenderer = spriteRenderer;
        }

        // 乗降状態を設定。
        public void SetRide(bool isRide) {
            SetCharacterRide(isRide, _moveSpeed);
        }

        private void Start() {
            //フレーム単位での処理
            TimeHandler.Instance.AddTimeActionEveryFrame(UpdateTimeHandler);
        }

        private void OnDestroy() {
            TimeHandler.Instance?.RemoveTimeAction(UpdateTimeHandler);
        }
    }
}
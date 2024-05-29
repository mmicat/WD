using System;
using UnityEngine;

namespace WitchDoctor.CoreResources.StateMachine
{
    public abstract class StateHistory : IState
    {
        public static T Get<T>() where T : StateHistory, new()
        {
            var state = new T();
            return state;
        }

        public static TState Get<TState>(Type exactType) where TState : StateHistory
        {
            var state = (TState) Activator.CreateInstance(exactType);
            return state;
        }

        protected virtual void OnSpawn()
        {

        }

        protected virtual void OnDespawn()
        {

        }


        public virtual void OnEnter()
        {

        }

        public virtual void OnUpdate()
        {

        }

        public virtual void OnExit()
        {

        }
    }

    public class StateHistory<TStateMachine, TState> : StateHistory
        where TStateMachine : StateMachineHistory<TStateMachine, TState>, new()
        where TState : StateHistory<TStateMachine, TState>
    {
        public TState PrevState { get; set; }
        public TState NextState { get; set; }
        public TStateMachine AssignedStateMachine { get; set; }
        private bool _enteredState;


        protected override void OnSpawn()
        {
            AssignedStateMachine = null;

            PrevState = null;
            NextState = null;

            _enteredState = false;
        }

        protected override void OnDespawn()
        {
            AssignedStateMachine = null;

            PrevState = null;
            NextState = null;

            if (_enteredState)
            {
                Debug.LogWarning("StateHistory | Had to clean up entered state OnDespawn");
                _enteredState = false;
            }
        }

        public void EnterState()
        {
            if (_enteredState)
            {
                Debug.LogWarning("StateHistory | EnterState called when state is already marksed as entered");
                return;
            }

            _enteredState = true;

            OnEnter();
        }

        public void ExitState()
        {
            if (!_enteredState)
            {
                Debug.LogWarning("StateHistory | ExitState called when state is already marked as exited");
                return;
            }

            _enteredState = false;

            OnExit();
        }

        public void CleanAllStates()
        {
            CleanPreviousStates();
            CleanNextStates();
        }

        public void CleanPreviousStates()
        {
            if (PrevState == null)
            {
                return;
            }

            // Break link with main list
            // Note : the previous state
            // of the previous state is
            // actually the current state
            TState cleanState = PrevState;
            PrevState = null;
            cleanState.PrevState = null;

            // Cleans up subsequent nodes recursively
            cleanState.CleanAllStates();
            cleanState = null;
        }

        public void CleanNextStates()
        {
            if (NextState == null)
            {
                return;
            }

            // Break link with main list
            TState cleanState = NextState;
            NextState = null;
            cleanState.PrevState = null;

            // Cleans up subsequent nodes recursively
            cleanState.CleanAllStates();
            cleanState = null;
        }

        public void CleanOldestPreviousState(int historyLength, int nthOldest = 0)
        {
            // Function assumes that nthOldest value
            // will always be smaller than or equal
            // to the history length
            if (PrevState == null)
            {
                return;
            }

            // Find oldest state
            TState cleanState = PrevState;
            int tempIter = 0;
            while (cleanState.PrevState != null && tempIter < historyLength - nthOldest)
            {
                cleanState = cleanState.PrevState;
                tempIter++;
            }

            // Break link with the list
            cleanState.NextState.PrevState = null;
            cleanState.NextState = null;

            cleanState.CleanAllStates();
            cleanState = null;
        }

        public void CleanOldestNextState(int historyLength, int nthOldest = 0)
        {
            // Function assumes that nthOldest value
            // will always be smaller than or equal
            // to the history length
            if (NextState == null)
            {
                return;
            }

            // Find oldest state
            TState cleanState = NextState;
            int tempIter = 0;
            while (cleanState.NextState != null && tempIter < historyLength - nthOldest)
            {
                cleanState = cleanState.NextState;
            }

            // Break link with the list
            cleanState.PrevState.NextState = null;
            cleanState.PrevState = null;

            cleanState.CleanAllStates();
            cleanState = null;
        }

    }

}
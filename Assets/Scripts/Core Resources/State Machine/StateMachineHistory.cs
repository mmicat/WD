using System;
using UnityEngine;

namespace WitchDoctor.CoreResources.StateMachine
{
    public interface IStateMachineHistory
    {
        int TotalNextStates { get; }
        int TotalPreviousStates { get; }

        bool IsOnFirstState { get; }
        bool IsOnLastState { get; }

        // Undo and Redo functionality
        // Maximum cap of 10
        void GoToNextState(int forwardCount);
        void GoToPreviousState(int backCount);

        // Functionality to set new states
        void GoToState<TStateType>();
    }

    public class StateMachine
    {
        private IState CurrentState { get; set; }

        public Action<StateMachine, IState, IState> OnStateChanged;

        private void GoTo(IState newState)
        {
            var prevState = CurrentState;

            CurrentState?.OnExit();
            CurrentState = newState;
            CurrentState?.OnEnter();

            OnStateChanged?.Invoke(this, CurrentState, prevState);
        }

        private void Update()
        {
            CurrentState?.OnUpdate();
        }
    }

    public class StateMachineHistory : IStateMachineHistory
    {
        // Applies to both sides so it's essentially twice the length
        protected const int HISTORY_LENGTH = 5;

        public virtual int TotalNextStates
        {
            get { return 0; }
        }
        public virtual int TotalPreviousStates
        {
            get { return 0; }
        }

        public virtual bool IsOnFirstState
        {
            get { return false; }
        }

        public virtual bool IsOnLastState
        {
            get { return false; }
        }

        public virtual void GoToNextState(int forwardCout)
        {
            throw new NotImplementedException("StateMachineHistory | GoToNextState() not implemented");
        }

        public virtual void GoToPreviousState(int backCount)
        {
            throw new NotImplementedException("StateMachineHistory | GoToPreviousState() not implemented");
        }

        public virtual void GoToState<TStateType>()
        {
            throw new NotImplementedException("StateMachineHistory | GoToState<TStateType>() not implemented");
        }
    }

    public class StateMachineHistory<TStateMachine, TState> : StateMachineHistory
        where TStateMachine : StateMachineHistory<TStateMachine, TState>, new()
        where TState : StateHistory<TStateMachine, TState>
    {
        private int _totalNextStates;
        private int _totalPreviousStates;
        protected TState _currentState;

        public TState CurrentState
        {
            get
            {
                return _currentState;
            }
        }

        public override int TotalNextStates
        {
            get
            {
                return _totalNextStates;
            }
        }

        public override int TotalPreviousStates
        {
            get
            {
                return _totalPreviousStates;
            }
        }

        public override bool IsOnLastState
        {
            get
            {
                return _totalNextStates == 0;
            }
        }

        public override bool IsOnFirstState
        {
            get
            {
                return _totalPreviousStates == 0;
            }
        }

        protected virtual TState GetState(Type stateType)
        {
            TState state = StateHistory.Get<TState>(stateType);
            state.AssignedStateMachine = (TStateMachine)this;
            return state;
        }

        public override void GoToState<TStateType>()
        {
            if (_currentState != null)
            {
                if (_totalPreviousStates >= HISTORY_LENGTH)
                {
                    _currentState.CleanOldestPreviousState(HISTORY_LENGTH);
                    _totalPreviousStates--;
                }

                TState nextState = GetState(typeof(TStateType));
                InsertAfterState(_currentState, nextState);
                InternalStateTransition(nextState);
                _totalPreviousStates++;
                return;
            }

            // States don't exist. Create new state
            InternalStateTransition(GetState(typeof(TStateType)));
        }

        public virtual void GoToStateNonHistorically<TStateType>(bool exitLast = true)
        {
            // Clean all states and insert a new state. Requires a context
            _currentState.CleanAllStates();
            _currentState = null;
            if (!exitLast) _currentState = null;
            _totalNextStates = 0;
            _totalPreviousStates = 0;
            InternalStateTransition(GetState(typeof(TStateType)));
        }

        public override void GoToPreviousState(int backCount = 1)
        {
            if (_currentState == null || backCount <= 0 || _totalPreviousStates <= 0)
            {
                return;
            }

            if (backCount > _totalPreviousStates)
            {
                // Go to first state
                backCount = _totalPreviousStates;
                _totalNextStates += _totalPreviousStates;
                _totalPreviousStates = 0;
                InternalMoveToState(-backCount);
                if (_totalNextStates >= HISTORY_LENGTH)
                {
                    _currentState.CleanOldestNextState(HISTORY_LENGTH, _totalNextStates - HISTORY_LENGTH);
                    _totalNextStates = HISTORY_LENGTH;
                }
                return;
            }

            _totalPreviousStates -= backCount;
            _totalNextStates += backCount;
            InternalMoveToState(-backCount);
            if (_totalNextStates >= HISTORY_LENGTH)
            {
                _currentState.CleanOldestNextState(HISTORY_LENGTH, _totalNextStates - HISTORY_LENGTH);
                _totalNextStates = HISTORY_LENGTH;
            }
        }

        public override void GoToNextState(int forwardCount = 1)
        {
            if (_currentState == null || forwardCount <= 0 || _totalNextStates <= 0)
            {
                return;
            }

            if (forwardCount > _totalNextStates)
            {
                // Go to last state
                forwardCount = _totalNextStates;
                _totalPreviousStates += _totalNextStates;
                _totalNextStates = 0;
                InternalMoveToState(forwardCount);
                if (_totalPreviousStates >= HISTORY_LENGTH)
                {
                    _currentState.CleanOldestPreviousState(HISTORY_LENGTH, _totalPreviousStates - HISTORY_LENGTH);
                    _totalPreviousStates = HISTORY_LENGTH;
                }
                return;
            }

            _totalNextStates -= forwardCount;
            _totalPreviousStates += forwardCount;
            InternalMoveToState(forwardCount);
            if (_totalPreviousStates >= HISTORY_LENGTH)
            {
                _currentState.CleanOldestPreviousState(HISTORY_LENGTH, _totalPreviousStates - HISTORY_LENGTH);
                _totalPreviousStates = HISTORY_LENGTH;
            }
        }

        private void InsertAfterState(TState targetState, TState insertState)
        {
            insertState.PrevState = targetState;
            insertState.NextState = targetState.NextState;

            if (targetState.NextState != null)
            {
                targetState.NextState.PrevState = insertState;
            }

            targetState.NextState = insertState;
        }

        private void InternalStateTransition(TState nextState)
        {
            InternalExitToEnterState(nextState);
        }

        private void InternalMoveToState(int movementCount)
        {
            TState tempState = _currentState;
            if (movementCount >= 0)
            {
                for (int i = movementCount; i > 0; i--)
                {
                    tempState = tempState.NextState;
                }
            }
            else
            {
                for (int i = movementCount; i < 0; i++)
                {
                    tempState = tempState.PrevState;
                }
            }

            InternalExitToEnterState(tempState);
        }

        private void InternalExitToEnterState(TState nextState)
        {
            CurrentState?.ExitState();

            Debug.Log(string.Format(
                "switching from state {0} to state {1}",
                CurrentState != null ? CurrentState.GetType().Name : "null",
                nextState != null ? nextState.GetType().Name : "null"
                )
            );

            if (nextState == null)
            {
                throw new NullReferenceException(
                    "StateMachineHistory | InternalExitToEnterState(nextState, context) nextState is not defined!");
            }

            _currentState = nextState;
            _currentState.EnterState();
        }
    }
}
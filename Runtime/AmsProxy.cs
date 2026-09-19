namespace TeaSpoons.AMS
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A node in the attribute hierarchy that holds containers and can have parent/children.
    /// Calculates values for itself, with parents, with children, or all combined.
    /// 
    /// Extend this class and override virtual methods to add custom behavior.
    /// </summary>
    public class AmsProxy
    {
        private readonly HashSet<AmsProxy> children = new();
        
        private readonly HashSet<AmsContainer> containers = new();
        public IReadOnlyCollection<AmsContainer> Containers => containers;

        private AmsProxy parent;

        private AmsValues values;
        public AmsValues Values => values;
        
        private AmsValues valuesWithParents;
        public AmsValues ValuesWithParents => valuesWithParents;
        
        private AmsValues valuesWithChildren;
        public AmsValues ValuesWithChildren => valuesWithChildren;
        
        private AmsValues valuesWithParentsAndChildren;
        public AmsValues ValuesWithParentsAndChildren => valuesWithParentsAndChildren;

        private bool changedSelf = true;
        private bool changedParents = true;
        private bool changedChildren = true;
        private bool changedParentsAndChildren = true;

        private readonly IAmsCalculator calculator;

        public AmsProxy() : this(AmsCalculatorProvider.Instance)
        {
        }

        public AmsProxy(IAmsCalculator calculator)
        {
            this.calculator = calculator ?? AmsCalculatorProvider.Instance;
            values = new AmsValues(this.calculator);
            valuesWithParents = new AmsValues(this.calculator);
            valuesWithChildren = new AmsValues(this.calculator);
            valuesWithParentsAndChildren = new AmsValues(this.calculator);
        }

        /// <summary>
        /// Gets the root of this proxy's hierarchy.
        /// Returns self if this is the root.
        /// </summary>
        public AmsProxy Root
        {
            get
            {
                var current = this;
                while (current.Parent != null)
                {
                    current = current.Parent;
                }
                
                return current;
            }
        }

        /// <summary>
        /// Gets the parent proxy, or null if this is a root.
        /// </summary>
        public AmsProxy Parent
        {
            get => parent;
            set => parent = value;
        }

        /// <summary>
        /// Gets all child proxies.
        /// </summary>
        public IReadOnlyCollection<AmsProxy> Children => children;

        /// <summary>
        /// Adds a child proxy to this proxy.
        /// If the child already has a parent, it will be removed from its current parent.
        /// </summary>
        public void AddChild(AmsProxy child)
        {
            if (child == null) return;
            
            if (!children.Add(child))
            {
                return;
            }

            // Remove from previous parent
            child.Parent?.RemoveChild(child);

            // Set new parent
            child.Parent = this;
            child.NotifyChanged(AmsChangedSource.Parent);

            NotifyChanged(AmsChangedSource.Child);
            
            OnChildAdded(child);
        }

        /// <summary>
        /// Removes a child proxy from this proxy.
        /// </summary>
        public void RemoveChild(AmsProxy child)
        {
            if (child == null) return;

            if (!children.Remove(child))
            {
                return;
            }

            child.Parent = null;
            child.NotifyChanged(AmsChangedSource.Parent);

            NotifyChanged(AmsChangedSource.Child);
            
            OnChildRemoved(child);
        }

        public bool IsRoot => Parent == null;
        public bool IsLeaf => children.Count == 0;

        public bool IsBranch => children.Count > 0;

        /// <summary>
        /// Returns this proxy and all descendants in a flattened enumeration, not deterministic.
        /// </summary>
        public IEnumerable<AmsProxy> Flattened()
        {
            yield return this;
            foreach (var child in children)
            {
                foreach (var descendant in child.Flattened())
                {
                    yield return descendant;
                }
            }
        }
        

        /// <summary>
        /// Adds a container to this proxy.
        /// Empty containers are skipped.
        /// </summary>
        public void AddContainer(AmsContainer container)
        {
            if (container == null || container.IsEmpty)
                return;
            
            if (!containers.Add(container))
            {
                return;
            }

            Logs.Debug($"Added container: {container}");

            NotifyChanged(AmsChangedSource.Self);
            
            OnContainerAdded(container);
        }

        public void RemoveContainer(AmsContainer container)
        {
            if (container == null) return;

            if (!containers.Remove(container))
            {
                return;
            }

            Logs.Debug($"Removed container: {container}");

            NotifyChanged(AmsChangedSource.Self);
            
            OnContainerRemoved(container);
        }


        /// <summary>
        /// Main update tick. Only call on root proxy.
        /// Updates all dirty values in the hierarchy.
        /// </summary>
        public void Tick()
        {
            if (!IsRoot) return;

            foreach (var proxy in Flattened())
            {
                proxy.UpdateValuesSelf();
            }

            UpdateValuesWithChildren(true);

            foreach (var proxy in Flattened().Where(p => p.IsLeaf))
            {
                proxy.UpdateValuesWithParents(true);
            }

            foreach (var proxy in Flattened())
            {
                proxy.UpdateValuesWithParentsAndChildren(false);
            }
            
            OnTick();
        }

        /// <summary>
        /// Updates this proxy's own values from its containers.
        /// </summary>
        public void UpdateValuesSelf()
        {
            if (!changedSelf) return;

            var oldValues = values;
            var newValues = new AmsValues(calculator);
            
            foreach (var container in containers)
            {
                newValues.ValueSet.AddValueSet(container.ValueSet, container.StackCount);
            }
            newValues.Calculate();

            values = newValues;
            changedSelf = false;
            
            OnValuesSelfChanged(oldValues, newValues);
        }

        /// <summary>
        /// Updates values including parent contributions.
        /// </summary>
        public void UpdateValuesWithParents(bool updateDependencies)
        {
            if (!changedParents) return;

            var newValues = new AmsValues(calculator);

            // Add parent's values first
            if (!IsRoot)
            {
                if (updateDependencies)
                {
                    Parent.UpdateValuesWithParents(true);
                }

                newValues.ValueSet.AddValueSet(Parent.ValuesWithParents.ValueSet);
            }

            // Add own values
            if (updateDependencies)
            {
                UpdateValuesSelf();
            }

            newValues.ValueSet.AddValueSet(values.ValueSet);
            newValues.Calculate();

            valuesWithParents = newValues;
            changedParents = false;
        }

        /// <summary>
        /// Updates values including children contributions.
        /// </summary>
        public void UpdateValuesWithChildren(bool updateDependencies)
        {
            if (!changedChildren)
                return;

            var newValues = new AmsValues(calculator);

            // Add children's values first
            foreach (var child in children)
            {
                if (updateDependencies)
                {
                    child.UpdateValuesWithChildren(true);
                }

                newValues.ValueSet.AddValueSet(child.ValuesWithChildren.ValueSet);
            }

            // Add own values
            if (updateDependencies)
            {
                UpdateValuesSelf();
            }

            newValues.ValueSet.AddValueSet(values.ValueSet);
            newValues.Calculate();

            valuesWithChildren = newValues;
            changedChildren = false;
        }

        /// <summary>
        /// Updates values including both parent and children contributions.
        /// </summary>
        public void UpdateValuesWithParentsAndChildren(bool updateDependencies)
        {
            if (!changedParentsAndChildren)
                return;

            var newValues = new AmsValues(calculator);

            // Add children's values
            foreach (var child in children)
            {
                if (updateDependencies)
                {
                    child.UpdateValuesWithChildren(true);
                }

                newValues.ValueSet.AddValueSet(child.ValuesWithChildren.ValueSet);
            }

            // Add own values with parents (optimization - includes our own values)
            if (updateDependencies)
            {
                UpdateValuesWithParents(true);
            }

            newValues.ValueSet.AddValueSet(valuesWithParents.ValueSet);
            newValues.Calculate();

            valuesWithParentsAndChildren = newValues;
            changedParentsAndChildren = false;
        }

        /// <summary>
        /// Notifies this proxy that values have changed.
        /// </summary>
        public void NotifyChanged(AmsChangedSource source)
        {
            switch (source)
            {
                case AmsChangedSource.Child:
                    changedChildren = true;
                    Parent?.NotifyChanged(AmsChangedSource.Child);
                    break;

                case AmsChangedSource.Parent:
                    changedParents = true;
                    foreach (var child in children)
                    {
                        child.NotifyChanged(AmsChangedSource.Parent);
                    }
                    break;

                case AmsChangedSource.Self:
                    changedSelf = true;
                    foreach (var child in children)
                    {
                        child.NotifyChanged(AmsChangedSource.Parent);
                    }
                    Parent?.NotifyChanged(AmsChangedSource.Child);
                    break;
            }

            changedParentsAndChildren = true;
        }

        #region Virtual Hooks

        /// <summary>
        /// Called after Tick() completes on the root proxy.
        /// Override to add post-tick logic.
        /// </summary>
        protected virtual void OnTick()
        {
        }

        /// <summary>
        /// Called when this proxy's own values have been recalculated.
        /// Override to react to value changes.
        /// </summary>
        protected virtual void OnValuesSelfChanged(AmsValues oldValues, AmsValues newValues)
        {
        }

        /// <summary>
        /// Called after a container is added to this proxy.
        /// </summary>
        protected virtual void OnContainerAdded(AmsContainer container)
        {
        }

        /// <summary>
        /// Called after a container is removed from this proxy.
        /// </summary>
        protected virtual void OnContainerRemoved(AmsContainer container)
        {
        }

        /// <summary>
        /// Called after a child proxy is added.
        /// </summary>
        protected virtual void OnChildAdded(AmsProxy child)
        {
        }

        /// <summary>
        /// Called after a child proxy is removed.
        /// </summary>
        protected virtual void OnChildRemoved(AmsProxy child)
        {
        }

        #endregion

        public override string ToString()
        {
            return $"AmsProxy [containers={containers.Count}, children={children.Count}, isRoot={IsRoot}]";
        }
    }
}

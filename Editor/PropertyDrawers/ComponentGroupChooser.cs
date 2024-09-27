using System.Linq;
using System.Reflection;

namespace ME.BECS.Editor {
    
    using UnityEngine.UIElements;

    [UnityEditor.CustomPropertyDrawer(typeof(ComponentGroupChooser))]
    public class ComponentGroupChooserDrawer : UnityEditor.PropertyDrawer {

        public struct Item {

            public string value;
            public System.Type type;
            public int index;

        }

        private static System.Collections.Generic.List<Item> componentGroups;
        private static System.Collections.Generic.List<Item> GetComponentGroups() {

            if (componentGroups != null) return componentGroups;

            var asms = CodeGenerator.GetAssembliesInfo();
            var list = new System.Collections.Generic.List<Item>();
            var idx = 0;
            var components = UnityEditor.TypeCache.GetTypesWithAttribute<ComponentGroupAttribute>();
            foreach (var component in components) {
                
                var asm = component.Assembly.GetName().Name;
                var info = asms.FirstOrDefault(x => x.name == asm);
                if (info.isEditor == true) continue;
                
                var attr = component.GetCustomAttribute<ComponentGroupAttribute>();
                list.Add(new Item() {
                    index = ++idx,
                    value = UnityEditor.ObjectNames.NicifyVariableName(attr.groupType.Name),
                    type = attr.groupType,
                });
            }
            componentGroups = list;
            return list;

        }

        public override VisualElement CreatePropertyGUI(UnityEditor.SerializedProperty property) {

            var root = new VisualElement();
            {
                var container = new VisualElement();
                container.AddToClassList("field");
                root.Add(container);
            
                var so = property.serializedObject;
                var prop = property;
                var choices = GetComponentGroups();
                Button nameField = null;
                nameField = new Button(() => {
                    var rect = nameField.worldBound;
                    var types = choices.Select(x => x.type).Distinct().ToArray();
                    EditorUtils.ShowPopup(rect, (type) => {
                        //var idx = System.Array.IndexOf(types, type);
                        so.Update();
                        //var val = evt.newValue;
                        var item = choices.FirstOrDefault(x => x.type == type);
                        prop.stringValue = type.AssemblyQualifiedName;
                        nameField.text = item.value;
                        //nameField.index = idx.index - 1;
                        so.ApplyModifiedProperties();
                        so.Update();
                    }, types, showNullElement: false);
                });
                var idx = choices.FirstOrDefault(x => x.type.AssemblyQualifiedName == prop.stringValue).index - 1;
                if (idx >= 0) {
                    nameField.text = choices[idx].value;
                } else {
                    nameField.text = "<none>";
                }
                //new DropdownField(choices.Select(x => x.value).ToList(), choices.FirstOrDefault(x => x.type.AssemblyQualifiedName == prop.stringValue).index - 1);
                /*nameField.RegisterCallback<ClickEvent>((evt) => {
                    
                });
                nameField.RegisterValueChangedCallback((evt) => {
                    so.Update();
                    var val = evt.newValue;
                    var idx = choices.FirstOrDefault(x => x.value == val);
                    prop.stringValue = idx.type.AssemblyQualifiedName;
                    nameField.index = idx.index - 1;
                    so.ApplyModifiedProperties();
                    so.Update();
                });*/
                container.Add(nameField);
            
                EditorUIUtils.DrawTooltip(container, property);
            }
            return root;

        }

    }

    
}
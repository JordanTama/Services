using Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public class ServiceWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Services";
        
        [MenuItem("JordanTama/" + WINDOW_NAME)]
        private static void ShowWindow()
        {
            var window = GetWindow<ServiceWindow>();
            window.titleContent = new GUIContent(WINDOW_NAME, EditorGUIUtility.IconContent("_Popup").image);
        }

        private void OnEnable()
        {
            // Listen to changes on the Locator
            Locator.ServiceRegistered += OnServiceChange;
            Locator.ServiceUnregistered += OnServiceChange;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            Locator.ServiceRegistered -= OnServiceChange;
            Locator.ServiceUnregistered -= OnServiceChange;
            
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void CreateGUI()
        {
            Draw();
        }

        private void OnServiceChange(IService service)
        {
            Draw();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange newState)
        {
            Draw();
        }

        private void Draw()
        {
            rootVisualElement.Clear();
            
            // Warning if the game isn't running
            var warning = new HelpBox("Run the game to see the registered Services.", HelpBoxMessageType.Warning)
            {
                style =
                {
                    display = Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex
                }
            };

            // List of services
            var titleLabel = new Label("Registered Services")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None
                }
            };
                
            var services = Locator.AllServices;
            var list = new ListView(Locator.AllServices, 20f, MakeItem, BindItem)
            {
                style =
                {
                    display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None
                }
            };

            var servicesElement = new VisualElement();
            servicesElement.Add(titleLabel);
            servicesElement.Add(list);

            rootVisualElement.Add(servicesElement);
            rootVisualElement.Add(warning);
            return;

            VisualElement MakeItem()
            {
                var label = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft
                    },
                    name = "nameLabel"
                };

                var typeLabel = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleRight,
                        color = new Color(0.5f, 0.5f, 0.5f, 1f),
                        unityFontStyleAndWeight = FontStyle.Italic
                    },
                    name = "typeLabel"
                };

                var element = new VisualElement
                {
                    style =
                    {
                        paddingLeft = 10f,
                        justifyContent = Justify.SpaceBetween,
                        flexDirection = FlexDirection.Row
                    }
                };
                element.Add(label);
                element.Add(typeLabel);

                return element;
            }

            void BindItem(VisualElement element, int index)
            {
                var nameLabel = element.Q<Label>("nameLabel");
                nameLabel.text = services[index].GetType().FullName;

                var typeLabel = element.Q<Label>("typeLabel");
                typeLabel.text = services[index] switch
                {
                    IServiceAsync => nameof(IServiceAsync),
                    IServiceStandard => nameof(IServiceStandard),
                    not null => nameof(IService),
                    _ => "INVALID TYPE"
                };
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServicesExcuter : MonoBehaviour
{
    [System.Serializable]
    public class ServiceInfo
    {
        public string name;
        [SerializeReference] public BaseService sender;
    }

    [SerializeField] private ServiceInfo[] _serviceInfo;

    public void Excute()
    {
        foreach(var info in _serviceInfo)
        {
            ServicesDispatch.Execute(info.sender.GetType(), info.name, info.sender);
        }
    }
}

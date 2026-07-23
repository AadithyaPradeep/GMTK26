using System.Collections;
using TMPro;
using UnityEngine;


public class Bomb : MonoBehaviour
{
    public float timer;
    public TextMeshPro text;
    public GameObject explosion;
    private bool dead = false;
   
    private void Update()
    {
        if(timer > 0)
        {
            text.text = Mathf.RoundToInt(timer).ToString();
            timer -= Time.deltaTime;
        }
        if(timer <= 0 && !dead)
        {
            dead = true;
            StartCoroutine("Spawn");
            GetComponent<SpriteRenderer>().enabled = false;
            text.gameObject.SetActive(false);
        }
    }
    private IEnumerator Spawn()
    {
       
        GameObject spawnS = Instantiate(explosion, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.7f);
        Destroy(spawnS);
        Destroy(this.gameObject);
    }

}

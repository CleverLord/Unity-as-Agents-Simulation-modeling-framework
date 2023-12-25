Shader "Custom/GameOfLife"
{
    Properties
    {
        _MainTex ("Current State", 2D) = "white" {}
        _NewStateTex ("New State", 2D) = "white" {}
        _Threshold ("Threshold", Range(0, 15)) = 0.5
        _Dims ("Dimensions", Vector) = (1, 1, 1, 1)
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _NewStateTex;
            float _Threshold;
            float4 _Dims;
            float4 _Offset;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Apply an offset equal to half of the pixel width
                const float2 pixelDims = 1.0 / _Dims.xy;
                const float2 pixelOffset = 0.5 / _Dims.xy;
                const float2 currentCoordsUV = i.uv - pixelOffset;

                float4 currentState = tex2D(_MainTex, currentCoordsUV);
                // Calculate the sum of neighboring pixel values
                float sum = 0.0;
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        float2 neighborUV = currentCoordsUV + float2(x * pixelDims.x, y * pixelDims.y);
                        if (neighborUV.x < 0 || neighborUV.x > 1 || neighborUV.y < 0 || neighborUV.y > 1)
                            continue;
                        sum += tex2D(_MainTex, neighborUV).r;
                    }
                }
                
                // Apply Conway's Game of Life rules
                float4 newState = currentState;
                //return newState;
                // return isAlive ? aliveNeighbours is 2 or 3 : aliveNeighbours is 3; // <- copied from C# code
                // To avoid rounding errors, we use 0.5 as the threshold
                // So, newState>0.5 means the cell is alive
                // sum < 1.5 means the cell has less than 1 or 0 alive neighbors
                // sum > 3.5 means the cell has 4 or more alive neighbors
                if (currentState.r > 0.5)
                {
                    // Cell is alive, we can color it black if it should die
                    if (sum < 1.5 || sum > _Threshold) // means sum is not 2 or 3
                    {
                        newState = float4(0, 0, 0, 1); // Cell dies
                    }
                }
                else
                {
                    //newState = float4(1, 1, 1, 1); // Debug, check if this is the problem
                    // Cell is dead, we can color it white if it should become alive
                    if (sum > 2.5 && sum < 3.5) //means sum is 3
                    {
                        newState = float4(1, 1, 1, 1); // Cell becomes alive
                    }
                }

                // Update _NewStateTex with the calculated new state
                return newState;
                //return new color based on coords for debugging. U is R, V is G
                //return float4(sum/8, 0, 0, 1);
            }
            ENDCG
        }
    }
}
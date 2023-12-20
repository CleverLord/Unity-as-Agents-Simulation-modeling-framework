Shader "Custom/GameOfLife"
{
    Properties
    {
        _MainTex ("Current State", 2D) = "white" {}
        _NewStateTex ("New State", 2D) = "white" {}
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

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 currentState = tex2D(_MainTex, uv);

                float2 offsets[8];
                offsets[0] = float2(-1, -1);
                offsets[1] = float2(0, -1);
                offsets[2] = float2(1, -1);
                offsets[3] = float2(-1, 0);
                offsets[4] = float2(1, 0);
                offsets[5] = float2(-1, 1);
                offsets[6] = float2(0, 1);
                offsets[7] = float2(1, 1);


                // Apply an offset equal to half of the pixel width
                float2 pixelOffset = 0.5 / _ScreenParams.xy;

                // Calculate the sum of neighboring pixel values
                float sum = 0.0;
                for (int j = 0; j < 8; j++)
                {
                    float2 neighborUV = uv + (offsets[j] + pixelOffset) / _ScreenParams.xy;
                    sum += tex2D(_MainTex, neighborUV).r;
                }

                // Apply Conway's Game of Life rules
                float4 newState = currentState;
                // return isAlive ? aliveNeighbours is 2 or 3 : aliveNeighbours is 3;
                // To avoid rounding errors, we use 0.5 as the threshold
                // So, newState>0.5 means the cell is alive
                // sum < 1.5 means the cell has less than 2 alive neighbors
                // sum > 2.5 means the cell has 3 or more alive neighbors
                if (currentState.r > 0.5)
                {
                    // Cell is alive, we can color it black if it should die
                    if (sum < 1.5 || sum > 3.5) // means sum is 0, 1, 4, 5, 6, 7, 8
                    {
                        newState = float4(0, 0, 0, 1); // Cell dies
                    }
                }
                else
                {
                    // Cell is dead, we can color it white if it should become alive
                    if (sum > 2.5 && sum < 3.5) //means sum is 3
                    {
                        newState = float4(1, 1, 1, 1); // Cell becomes alive
                    }
                }

                // Update _NewStateTex with the calculated new state
                return newState;
            }
            ENDCG
        }
    }
}
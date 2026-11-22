import 'dotenv/config';

if (!process.env.OPENROUTER_API_KEY) {
  throw new Error('OPENROUTER_API_KEY is not defined in the environment variables');
}

async function main() {
  const res = await fetch('https://openrouter.ai/api/v1/chat/completions', {
    method: 'POST',
    headers: {
      Authorization: 'Bearer ' + process.env.OPENROUTER_API_KEY,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      model: 'nvidia/nemotron-3.5-super-vl:free',
      reasoning: { effort: 'medium' },
      messages: [
        {
          role: 'user',
          content: 'What is the meaning of life?',
        },
      ],
    }),
  });

  if (!res.ok) {
    throw new Error(`OpenRouter request failed: ${res.status} ${await res.text()}`);
  }

  const data: any = await res.json();
  const message = data.choices?.[0]?.message;
  console.log('*********************************** REASONING OUTPUT START ***********************************');
  console.log(message?.reasoning ?? '(none returned)');
  console.log('*********************************** ANSWER OUTPUT START **************************************');
  console.log(message?.content);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});

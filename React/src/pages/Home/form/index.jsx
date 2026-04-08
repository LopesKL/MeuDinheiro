import { Card } from '@components/Layout';
import { DynamicForm } from '@components/Data';
import { Button, message } from 'antd';
import { useRef } from 'react';

const FormPage = () => {
  const formRef = useRef(null);

  const formConfig = [
    {
      columns: 2,
      questions: [
        {
          type: 'text',
          id: 'nome',
          label: 'Nome',
          placeholder: 'Digite o nome',
          required: true,
        },
        {
          type: 'email',
          id: 'email',
          label: 'Email',
          placeholder: 'Digite o email',
          required: true,
        },
        {
          type: 'phone',
          id: 'telefone',
          label: 'Telefone',
          placeholder: 'Digite o telefone',
          required: true,
        },
        {
          type: 'cpf',
          id: 'cpf',
          label: 'CPF',
          placeholder: 'Digite o CPF',
          required: true,
        },
        {
          type: 'date',
          id: 'dataNascimento',
          label: 'Data de Nascimento',
          required: true,
        },
        {
          type: 'select',
          id: 'cidade',
          label: 'Cidade',
          placeholder: 'Selecione a cidade',
          options: [
            { label: 'São Paulo', value: 'sp' },
            { label: 'Rio de Janeiro', value: 'rj' },
            { label: 'Belo Horizonte', value: 'bh' },
          ],
        },
        {
          type: 'textarea',
          id: 'observacoes',
          label: 'Observações',
          placeholder: 'Digite as observações',
          rows: 4,
        },
      ],
    },
  ];

  const handleSubmit = (values) => {
    message.success('Formulário enviado com sucesso!');
    console.log('Valores:', values);
  };

  return (
    <Card title="Exemplo de Formulário Dinâmico">
      <DynamicForm ref={formRef} formConfig={formConfig} onSubmit={handleSubmit} />
      <Button
        type="primary"
        onClick={() => formRef.current?.submit()}
        style={{ marginTop: 16 }}
      >
        Enviar
      </Button>
    </Card>
  );
};

export default FormPage;

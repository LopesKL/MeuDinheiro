import { Card } from '@components/Layout';
import { DynamicForm } from '@components/Data';
import { message } from 'antd';

const FormBuilderPage = () => {
  const formConfig = [
    {
      columns: 1,
      questions: [
        {
          type: 'text',
          id: 'titulo',
          label: 'Título do Formulário',
          placeholder: 'Digite o título',
          required: true,
        },
        {
          type: 'textarea',
          id: 'descricao',
          label: 'Descrição',
          placeholder: 'Digite a descrição',
          rows: 4,
        },
      ],
    },
  ];

  const handleSubmit = (values) => {
    message.success('Formulário criado com sucesso!');
    console.log('Configuração do formulário:', values);
  };

  return (
    <Card title="Form Builder">
      <p>Use este formulário para criar configurações de formulários dinâmicos.</p>
      <DynamicForm formConfig={formConfig} onSubmit={handleSubmit} />
    </Card>
  );
};

export default FormBuilderPage;
